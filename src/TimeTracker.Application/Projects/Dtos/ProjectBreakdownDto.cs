namespace TimeTracker.Application.Projects.Dtos;

public record ProjectBreakdownDto(
    Guid UserId, 
    string EmployeeName, 
    double TotalHours
);