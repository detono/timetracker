using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.DeactivateUser;

/// <summary>
/// Employer-only: soft-deletes a user account by deactivating it. Deactivated users
/// can no longer log in, but their historical time entries and reports are preserved
/// (hard-deleting a user would either orphan or cascade-delete their logged hours,
/// which is never what you want for something payroll-adjacent).
/// </summary>
public record DeactivateUserCommand(Guid UserId) : IRequest<Result>;
