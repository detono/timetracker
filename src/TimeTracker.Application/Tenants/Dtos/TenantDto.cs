namespace TimeTracker.Application.Tenants.Dtos;

public record TenantDto(Guid Id, string Name, bool IsActive, int UserCount, DateTime CreatedAtUtc);
