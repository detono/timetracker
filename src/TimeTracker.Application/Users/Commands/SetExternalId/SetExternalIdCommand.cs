using FluentValidation;
using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Commands.SetExternalId;

/// <summary>
/// Employer-only: sets someone's id in another system (payroll, HR), sent along in webhooks.
/// Empty clears it. Two people in the company can't share one.
/// </summary>
public record SetExternalIdCommand(Guid UserId, string? ExternalId) : IRequest<Result>;

public class SetExternalIdCommandValidator : AbstractValidator<SetExternalIdCommand> {
    public SetExternalIdCommandValidator() {
        RuleFor(x => x.ExternalId).MaximumLength(ExternalIds.MaxLength);
    }
}

public class SetExternalIdCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<SetExternalIdCommand, Result> {
    public async Task<Result> Handle(SetExternalIdCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result.Failure("Only an employer can change this.", ResultErrorType.Forbidden);
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) {
            return Result.Failure("User was not found.", ResultErrorType.NotFound);
        }

        var externalId = ExternalIds.Normalize(request.ExternalId);
        if (externalId is not null) {
            var others = await unitOfWork.Users.GetAllAsync(cancellationToken);
            if (others.Any(u => u.Id != user.Id && string.Equals(u.ExternalId, externalId, StringComparison.OrdinalIgnoreCase))) {
                return Result.Failure("Someone else in the company already has this external ID.", ResultErrorType.Conflict);
            }
        }

        user.SetExternalId(externalId);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
