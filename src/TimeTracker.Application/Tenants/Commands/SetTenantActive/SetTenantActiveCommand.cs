using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Tenants.Commands.SetTenantActive;

/// <summary>
/// Deactivating a tenant blocks new logins for all of its users; nothing is deleted.
/// Tokens already issued stay valid until they expire.
/// </summary>
public record SetTenantActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;
