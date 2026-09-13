using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(string Name, string? ClientName) : IRequest<Result<Guid>>;