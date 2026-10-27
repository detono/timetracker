using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;

namespace TimeTracker.Application.PlatformAdmins.Queries.GetPlatformAdmins;

public record GetPlatformAdminsQuery : IRequest<Result<IReadOnlyList<UserDto>>>;
