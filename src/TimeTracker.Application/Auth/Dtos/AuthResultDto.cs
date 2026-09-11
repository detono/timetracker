namespace TimeTracker.Application.Auth.Dtos;

public record AuthResultDto(string Token, Guid UserId, string FullName, string Role);
