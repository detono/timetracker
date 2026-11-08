using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Projects.Dtos;

namespace TimeTracker.Application.Projects.Queries;

/// <summary>Hours per person on a project, over all time or only from <see cref="From"/> to <see cref="To"/>.</summary>
public record GetProjectBreakdownQuery(Guid ProjectId, DateOnly? From = null, DateOnly? To = null)
    : IRequest<Result<IReadOnlyList<ProjectBreakdownDto>>>;
