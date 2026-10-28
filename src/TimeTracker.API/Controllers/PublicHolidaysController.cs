using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.PublicHolidays.Commands.AddPublicHoliday;
using TimeTracker.Application.PublicHolidays.Commands.DeletePublicHoliday;
using TimeTracker.Application.PublicHolidays.Commands.ImportPublicHolidays;
using TimeTracker.Application.PublicHolidays.Dtos;
using TimeTracker.Application.PublicHolidays.Queries.GetPublicHolidays;

namespace TimeTracker.API.Controllers;

/// <summary>
/// The company's public holidays and days off. Nobody is expected to work on them, and leave
/// over them doesn't use up the balance.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Route("api/public-holidays")]
public class PublicHolidaysController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicHolidayDto>>> GetAll([FromQuery] int year, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPublicHolidaysQuery(year), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Adds a holiday or company day off by hand. Employer only.</summary>
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<PublicHolidayDto>> Add(AddPublicHolidayCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Adds the official holidays of the company's country (and region) for a year. Employer only.</summary>
    [HttpPost("import")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<PublicHolidayImportDto>> Import([FromQuery] int year, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ImportPublicHolidaysCommand(year), cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeletePublicHolidayCommand(id), cancellationToken);
        return HandleResult(result);
    }
}
