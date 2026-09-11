using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResultDto>>;
