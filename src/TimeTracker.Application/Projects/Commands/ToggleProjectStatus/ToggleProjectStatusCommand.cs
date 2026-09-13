using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Projects.Commands.ToggleProjectStatus;

public record ToggleProjectStatusCommand(Guid Id, bool IsActive) : IRequest<Result>;