using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.SignOut;

/// <summary>
/// Ends a device's sign-in by revoking its refresh token (and every earlier one from the same
/// sign-in). Needs only the refresh token, so it works even after the sign-in token expired.
/// Always succeeds, so it doesn't reveal whether a token existed.
/// </summary>
public record SignOutCommand(string RefreshToken) : IRequest<Result>;
