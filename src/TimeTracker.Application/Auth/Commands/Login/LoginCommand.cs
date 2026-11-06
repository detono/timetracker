using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.Login;

/// <param name="IssueRefreshToken">
/// True for clients that keep people signed in (the mobile app): the answer then includes a
/// refresh token. The web app leaves it off.
/// </param>
public record LoginCommand(string Email, string Password, bool IssueRefreshToken = false) : IRequest<Result<AuthResultDto>>;
