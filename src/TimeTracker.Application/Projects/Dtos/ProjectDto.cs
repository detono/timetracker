namespace TimeTracker.Application.Projects.Dtos;

public record ProjectDto(Guid Id, string Name, string? ClientName, bool IsActive);