using MediatR;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Maps an Application-layer <see cref="Result"/> onto the appropriate HTTP status code.</summary>
    protected ActionResult HandleResult(Result result)
    {
        if (result.Succeeded)
        {
            return NoContent();
        }

        return ToErrorResult(result.ErrorType, result.Error);
    }

    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToErrorResult(result.ErrorType, result.Error);
    }

    private ActionResult ToErrorResult(ResultErrorType errorType, string? error) => errorType switch
    {
        ResultErrorType.NotFound => NotFound(new { error }),
        ResultErrorType.Forbidden => new ObjectResult(new { error }) { StatusCode = StatusCodes.Status403Forbidden },
        ResultErrorType.Conflict => Conflict(new { error }),
        _ => BadRequest(new { error })
    };
}
