namespace TimeTracker.Application.Tenants.Dtos;

/// <summary>An employer account of a company, as a platform admin sees it: who it is, nothing of their data.</summary>
public record TenantEmployerDto(Guid Id, string FullName, string Email, bool IsActive);
