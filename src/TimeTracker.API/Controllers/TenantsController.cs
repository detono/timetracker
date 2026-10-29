using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Tenants.Commands.CreateTenant;
using TimeTracker.Application.Tenants.Commands.ResetEmployerPassword;
using TimeTracker.Application.Tenants.Commands.SetTenantActive;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Application.Tenants.Queries.GetTenantEmployers;
using TimeTracker.Application.Tenants.Queries.GetTenants;

namespace TimeTracker.API.Controllers;

/// <summary>Tenant (company) administration. Every endpoint here is PlatformAdmin-only.</summary>
[Authorize(Roles = "PlatformAdmin")]
public class TenantsController : ApiControllerBase
{
    /// <summary>Lists every tenant with its number of users.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTenantsQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Creates a tenant with its first Employer account and default hour types.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantDto>> Create(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Blocks login for every user of the tenant. Nothing is deleted.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetTenantActiveCommand(id, false), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Restores login for a previously deactivated tenant.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetTenantActiveCommand(id, true), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>The company's employer accounts (name and email only).</summary>
    [HttpGet("{id:guid}/employers")]
    public async Task<ActionResult<IReadOnlyList<TenantEmployerDto>>> GetEmployers(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTenantEmployersQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Sets a temporary password for one of the company's employers.</summary>
    [HttpPost("{id:guid}/employers/{userId:guid}/reset-password")]
    public async Task<ActionResult> ResetEmployerPassword(
        Guid id, Guid userId, [FromBody] ResetPasswordRequestBody body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ResetEmployerPasswordCommand(id, userId, body.NewPassword), cancellationToken);
        return HandleResult(result);
    }
}
