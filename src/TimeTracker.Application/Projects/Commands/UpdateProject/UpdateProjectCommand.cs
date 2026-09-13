using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Projects.Commands.UpdateProject;

public record UpdateProjectCommand(Guid Id, string Name, string? ClientName) : IRequest<Result>;