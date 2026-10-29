using System.Text.Json;
using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.AuditLog;

/// <param name="ActorRole">"Employer", "Employee" or "PlatformAdmin".</param>
/// <param name="Action">An <see cref="AuditAction"/> name, e.g. "MonthUnlocked".</param>
/// <param name="Details">Extra facts per action, e.g. "month" or "before"/"after".</param>
public record AuditEntryDto(
    Guid Id,
    DateTime AtUtc,
    string ActorName,
    string ActorRole,
    string Action,
    string? SubjectName,
    IReadOnlyDictionary<string, string> Details);

/// <summary>Employer only: the company's activity log, newest first, a page at a time.</summary>
/// <param name="Before">Only entries older than this; pass the last entry's time to load the next page.</param>
public record GetAuditLogQuery(DateTime? Before = null, int Take = 50) : IRequest<Result<IReadOnlyList<AuditEntryDto>>>;

public class GetAuditLogQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetAuditLogQuery, Result<IReadOnlyList<AuditEntryDto>>> {
    public const int MaxTake = 200;

    public async Task<Result<IReadOnlyList<AuditEntryDto>>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<IReadOnlyList<AuditEntryDto>>.Failure(
                "Only an employer can see the activity log.", ResultErrorType.Forbidden);
        }

        var take = Math.Clamp(request.Take, 1, MaxTake);
        var entries = await unitOfWork.AuditEntries.GetPageAsync(request.Before, take, cancellationToken);

        return Result<IReadOnlyList<AuditEntryDto>>.Success(entries.Select(e => new AuditEntryDto(
            e.Id,
            e.CreatedAtUtc,
            e.ActorName,
            e.ActorRole.ToString(),
            e.Action.ToString(),
            e.SubjectName,
            e.Details is null
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(e.Details) ?? new Dictionary<string, string>()
        )).ToList());
    }
}
