using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.SetTracksHours;

/// <summary>Employer-only: sets whether someone's logged hours are compared with their work schedule.</summary>
public record SetTracksHoursCommand(Guid UserId, bool TracksHours) : IRequest<Result>;
