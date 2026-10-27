using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Dtos;

namespace TimeTracker.Application.Tenants.Queries.GetTenants;

public record GetTenantsQuery : IRequest<Result<IReadOnlyList<TenantDto>>>;
