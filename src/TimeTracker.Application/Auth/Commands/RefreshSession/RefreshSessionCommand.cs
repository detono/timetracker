using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.RefreshSession;

/// <summary>
/// Swaps a refresh token for a new sign-in token and a new refresh token, without the
/// password. The old refresh token stops working.
/// </summary>
public record RefreshSessionCommand(string RefreshToken) : IRequest<Result<AuthResultDto>>;
