using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;

namespace TimeTracker.Application.HourTypes.Queries.GetHourTypes;

/// <summary>
/// Lists hour types. Any authenticated user can call this (they need it to log hours),
/// but only an Employer can request the inactive ones too (for the management page).
/// </summary>
public record GetHourTypesQuery(bool IncludeInactive) : IRequest<Result<IReadOnlyList<HourTypeDto>>>;
