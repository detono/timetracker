using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.RequestPasswordReset;

/// <summary>
/// "Forgot password": emails a reset code to the account, if it exists and its company has
/// email set up. Always succeeds, so the response doesn't reveal whether an email address
/// has an account.
/// </summary>
public record RequestPasswordResetCommand(string Email) : IRequest<Result>;
