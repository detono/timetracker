using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;
using TimeTracker.Application.TimeEntries.Commands.DeleteTimeEntry;
using TimeTracker.Application.TimeEntries.Commands.UpdateTimeEntry;
using TimeTracker.Application.TimeEntries.Dtos;
using TimeTracker.Application.TimeEntries.Queries.GetTeamTimeEntries;
using TimeTracker.Application.TimeEntries.Queries.GetTimeEntries;

namespace TimeTracker.API.Controllers;

[Authorize]
public class TimeEntriesController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public TimeEntriesController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>Logs a new time entry for the current user (or, for Employers, on behalf of another user).</summary>
    [HttpPost]
    public async Task<ActionResult<TimeEntryDto>> Create(CreateTimeEntryCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Updates an existing time entry the caller owns, or any entry if the caller is an Employer.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TimeEntryDto>> Update(Guid id, UpdateTimeEntryCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "Route id and body id must match." });
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Deletes a time entry.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteTimeEntryCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Gets entries for the current user. Pass userId to view someone else's (requires authority).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TimeEntryDto>>> GetMine(
        [FromQuery] Guid? userId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var target = userId ?? _currentUser.UserId;
        var result = await Mediator.Send(new GetTimeEntriesQuery(target, from, to), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets entries across every user the caller has authority over (their team, or
    /// everyone for an Employer). Powers the planboard view.
    /// </summary>
    [HttpGet("team")]
    public async Task<ActionResult<IReadOnlyList<TimeEntryDto>>> GetTeam(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTeamTimeEntriesQuery(from, to), cancellationToken);
        return HandleResult(result);
    }
}
