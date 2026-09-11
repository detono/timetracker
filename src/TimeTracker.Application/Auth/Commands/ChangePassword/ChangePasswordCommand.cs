using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.ChangePassword;

/// <summary>Self-service password change: any authenticated user may change their own password.</summary>
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;
