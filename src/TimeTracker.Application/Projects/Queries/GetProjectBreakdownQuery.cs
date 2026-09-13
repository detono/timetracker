using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Projects.Dtos;

namespace TimeTracker.Application.Projects.Queries;

public record GetProjectBreakdownQuery(Guid ProjectId) : IRequest<Result<IReadOnlyList<ProjectBreakdownDto>>>;