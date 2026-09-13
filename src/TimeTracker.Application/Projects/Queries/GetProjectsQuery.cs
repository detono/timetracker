using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Projects.Dtos;

namespace TimeTracker.Application.Projects.Queries;

public record GetProjectsQuery(bool IncludeInactive = false) : IRequest<Result<IReadOnlyList<ProjectDto>>>;