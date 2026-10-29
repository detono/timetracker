using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Self-service password change: any authenticated user may change their own password. Signs
/// out their other sessions and returns a fresh sign-in for this one. While a temporary
/// password must be replaced, the current password isn't asked again.
/// </summary>
public record ChangePasswordCommand(string? CurrentPassword, string NewPassword) : IRequest<Result<AuthResultDto>>;
