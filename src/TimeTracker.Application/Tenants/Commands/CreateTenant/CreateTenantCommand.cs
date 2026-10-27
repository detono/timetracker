using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Dtos;

namespace TimeTracker.Application.Tenants.Commands.CreateTenant;

/// <summary>
/// Creates a tenant together with its first Employer account (who then manages everyone
/// else in that tenant) and a starter set of hour types.
/// </summary>
public record CreateTenantCommand(
    string Name,
    string EmployerFirstName,
    string EmployerLastName,
    string EmployerEmail,
    string EmployerPassword) : IRequest<Result<TenantDto>>;
