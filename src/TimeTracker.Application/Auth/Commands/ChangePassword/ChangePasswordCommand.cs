namespace TimeTracker.Application.Auth.Commands.ChangePassword;

using MediatR;
using TimeTracker.Application.Common.Models;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;