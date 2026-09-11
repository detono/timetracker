using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Dtos;

namespace TimeTracker.Application.TimeEntries.Queries.GetTeamTimeEntries;

/// <summary>
/// Retrieves time entries across every employee the caller has authority over
/// (their supervisees, or everyone if the caller is an Employer). Backs the planboard view.
/// </summary>
public record GetTeamTimeEntriesQuery(DateOnly? From, DateOnly? To) : IRequest<Result<IReadOnlyList<TimeEntryDto>>>;
