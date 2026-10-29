using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.ResetPassword;

/// <summary>
/// Employer-only: sets a temporary password for another account without needing the old
/// one, e.g. when someone forgot theirs and the company has no email set up. The employer
/// hands it over in person; the user is signed out everywhere and must choose their own
/// password at their next login.
/// </summary>
public record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>;
