using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.ActivateUser;

/// <summary>Employer-only: reinstates a previously deactivated account.</summary>
public record ActivateUserCommand(Guid UserId) : IRequest<Result>;
