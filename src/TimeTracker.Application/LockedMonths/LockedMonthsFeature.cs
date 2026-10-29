using FluentValidation;
using MediatR;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LockedMonths;

/// <param name="LockedByName">Who locked it; "Unknown" if that account no longer exists.</param>
public record LockedMonthDto(int Year, int Month, string LockedByName, DateTime LockedAtUtc);

/// <summary>Every locked month of the company. Everyone may see them, so the app can show what can't be changed.</summary>
public record GetLockedMonthsQuery : IRequest<Result<IReadOnlyList<LockedMonthDto>>>;

/// <summary>Employer only: makes a month's hours final.</summary>
public record LockMonthCommand(int Year, int Month) : IRequest<Result<LockedMonthDto>>;

/// <summary>Employer only: allows changes to a month's hours again.</summary>
public record UnlockMonthCommand(int Year, int Month) : IRequest<Result>;

public class LockMonthCommandValidator : AbstractValidator<LockMonthCommand> {
    public LockMonthCommandValidator() {
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(2000, 9999);
    }
}

public class LockedMonthsHandlers(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    AuditTrail audit
) : IRequestHandler<GetLockedMonthsQuery, Result<IReadOnlyList<LockedMonthDto>>>,
    IRequestHandler<LockMonthCommand, Result<LockedMonthDto>>,
    IRequestHandler<UnlockMonthCommand, Result> {
    public async Task<Result<IReadOnlyList<LockedMonthDto>>> Handle(GetLockedMonthsQuery request, CancellationToken cancellationToken) {
        var months = await unitOfWork.LockedMonths.GetAllAsync(cancellationToken);
        var users = (await unitOfWork.Users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
        return Result<IReadOnlyList<LockedMonthDto>>.Success(months.Select(m => ToDto(m, users)).ToList());
    }

    public async Task<Result<LockedMonthDto>> Handle(LockMonthCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<LockedMonthDto>.Failure("Only an employer can lock a month.", ResultErrorType.Forbidden);
        }

        var users = (await unitOfWork.Users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
        var existing = await unitOfWork.LockedMonths.GetAsync(request.Year, request.Month, cancellationToken);
        if (existing is not null) {
            return Result<LockedMonthDto>.Success(ToDto(existing, users));
        }

        var locked = LockedMonth.Create(request.Year, request.Month, currentUser.UserId, clock.TodayUtc);
        await unitOfWork.LockedMonths.AddAsync(locked, cancellationToken);
        await audit.RecordAsync(AuditAction.MonthLocked, null, MonthDetails(request.Year, request.Month), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LockedMonthDto>.Success(ToDto(locked, users));
    }

    public async Task<Result> Handle(UnlockMonthCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result.Failure("Only an employer can unlock a month.", ResultErrorType.Forbidden);
        }

        var locked = await unitOfWork.LockedMonths.GetAsync(request.Year, request.Month, cancellationToken);
        if (locked is not null) {
            unitOfWork.LockedMonths.Remove(locked);
            await audit.RecordAsync(AuditAction.MonthUnlocked, null, MonthDetails(request.Year, request.Month), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private static Dictionary<string, string> MonthDetails(int year, int month) =>
        new() { ["month"] = $"{year:D4}-{month:D2}" };

    private static LockedMonthDto ToDto(LockedMonth month, IReadOnlyDictionary<Guid, User> users) =>
        new(month.Year, month.Month,
            users.TryGetValue(month.LockedByUserId, out var user) ? $"{user.FirstName} {user.LastName}" : "Unknown",
            month.CreatedAtUtc);
}
