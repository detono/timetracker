namespace TimeTracker.Application.Users.Dtos;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    Guid? SupervisorId,
    bool IsActive);
