using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.ResetPassword;

/// <summary>
/// Employer-only: sets a new password for another account without needing the old one -
/// e.g. when someone forgets their temporary password. The employer should hand the new
/// value to that person out of band; there's no "forgot password" email flow in this sample.
/// </summary>
public record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>;
