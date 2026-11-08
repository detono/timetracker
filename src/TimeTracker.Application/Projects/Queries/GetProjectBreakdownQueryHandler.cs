using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Projects.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects.Queries;

public class GetProjectBreakdownQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService
) : IRequestHandler<GetProjectBreakdownQuery, Result<IReadOnlyList<ProjectBreakdownDto>>>
{
    public async Task<Result<IReadOnlyList<ProjectBreakdownDto>>> Handle(GetProjectBreakdownQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.Role != UserRole.Employer)
            return Result<IReadOnlyList<ProjectBreakdownDto>>.Failure("Forbidden", ResultErrorType.Forbidden);

        if (request.From > request.To)
            return Result<IReadOnlyList<ProjectBreakdownDto>>.Failure("'To' date must not be before 'From' date.");

        var entries = await unitOfWork.TimeEntries.GetByProjectIdAsync(
            request.ProjectId, request.From, request.To, cancellationToken);
        
        var users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        var userDict = users.ToDictionary(u => u.Id);

        var breakdown = entries
            .GroupBy(e => e.UserId)
            .Select(g => 
            {
                userDict.TryGetValue(g.Key, out var user);
                return new ProjectBreakdownDto(
                    g.Key,
                    user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    Math.Round(g.Sum(e => e.Duration.TotalHours), 2)
                );
            })
            .OrderByDescending(b => b.TotalHours)
            .ToList();

        return Result<IReadOnlyList<ProjectBreakdownDto>>.Success(breakdown);
    }
}