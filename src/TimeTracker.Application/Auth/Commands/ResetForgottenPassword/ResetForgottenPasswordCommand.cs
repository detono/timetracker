using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.ResetForgottenPassword;

/// <summary>
/// Sets a new password with a code from <c>RequestPasswordResetCommand</c>. Signs out every
/// existing session; the user then signs in with the new password.
/// </summary>
public record ResetForgottenPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result>;
