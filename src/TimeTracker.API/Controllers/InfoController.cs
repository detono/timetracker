using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeTracker.API.Controllers;

/// <summary>
/// Public, sign-in free description of this server, so a client (like a phone app) can check
/// that an address the user typed really is a TimeTracker server, and one it can talk to.
/// </summary>
[AllowAnonymous]
public class InfoController : ApiControllerBase
{
    /// <summary>
    /// Goes up by one only when the API changes in a way that breaks existing clients.
    /// Adding endpoints or fields doesn't change it.
    /// </summary>
    public const int ApiVersion = 1;

    [HttpGet]
    public ActionResult<ServerInfoDto> Get() => Ok(new ServerInfoDto("TimeTracker", ApiVersion));
}

public record ServerInfoDto(string Name, int ApiVersion);
