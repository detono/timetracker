using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;

namespace TimeTracker.Application.PlatformAdmins.Commands.CreatePlatformAdmin;

public record CreatePlatformAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<Result<UserDto>>;
