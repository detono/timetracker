using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;

namespace TimeTracker.Application.Users.Queries.GetUsers;

/// <summary>Employer-only: lists every user account for administration and reporting.</summary>
public record GetUsersQuery : IRequest<Result<IReadOnlyList<UserDto>>>;
