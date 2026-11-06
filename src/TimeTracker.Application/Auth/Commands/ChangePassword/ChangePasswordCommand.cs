using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Self-service password change: any authenticated user may change their own password. Signs
/// out their other sessions and returns a fresh sign-in for this one. While a temporary
/// password must be replaced, the current password isn't asked again. The mobile app sets
/// <paramref name="IssueRefreshToken"/>, since the change also ends its old refresh token.
/// </summary>
public record ChangePasswordCommand(string? CurrentPassword, string NewPassword, bool IssueRefreshToken = false)
    : IRequest<Result<AuthResultDto>>;
