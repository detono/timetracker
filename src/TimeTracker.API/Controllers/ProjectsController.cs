using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Projects.Commands.CreateProject;
using TimeTracker.Application.Projects.Commands.ToggleProjectStatus;
using TimeTracker.Application.Projects.Commands.UpdateProject;
using TimeTracker.Application.Projects.Queries;
using TimeTracker.Application.Projects.Dtos;

namespace TimeTracker.API.Controllers;

[Authorize]
public class ProjectsController : ApiControllerBase {
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> Get([FromQuery] bool includeInactive = false) {
        return HandleResult(await Mediator.Send(new GetProjectsQuery(includeInactive)));
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<Guid>> Create(CreateProjectCommand command) {
        return HandleResult(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> Update(Guid id, UpdateProjectCommand command) {
        if (id != command.Id) {
            return BadRequest("The ID in the URL must match the ID in the request body.");
        }

        return HandleResult(await Mediator.Send(command));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult> ToggleStatus(Guid id, [FromBody] bool isActive) {
        return HandleResult(await Mediator.Send(new ToggleProjectStatusCommand(id, isActive)));
    }
    
    [HttpGet("{id:guid}/breakdown")]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<IReadOnlyList<ProjectBreakdownDto>>> GetBreakdown(Guid id, CancellationToken cancellationToken) {
        return HandleResult(await Mediator.Send(new GetProjectBreakdownQuery(id), cancellationToken));
    }
}