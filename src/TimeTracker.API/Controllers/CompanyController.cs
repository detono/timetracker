using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.Company.Commands.UpdateCompany;
using TimeTracker.Application.Company.Dtos;
using TimeTracker.Application.Company.Queries.GetCompany;

namespace TimeTracker.API.Controllers;

/// <summary>The caller's own company: name and address. Employer only.</summary>
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[Authorize(Roles = "Employer")]
[Route("api/company")]
public class CompanyController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyDto>> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCompanyQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Renames the company and sets its address. The country decides which public holidays apply.</summary>
    [HttpPut]
    public async Task<ActionResult<CompanyDto>> Update(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
