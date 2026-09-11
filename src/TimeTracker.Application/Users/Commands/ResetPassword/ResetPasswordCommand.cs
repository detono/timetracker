using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.ResetPassword;

public record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>;