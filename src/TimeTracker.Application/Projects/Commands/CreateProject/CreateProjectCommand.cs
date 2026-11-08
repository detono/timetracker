using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Projects.Commands.CreateProject;

/// <param name="MemberIds">Who may log hours on it. Empty or left out means everyone.</param>
public record CreateProjectCommand(
    string Name,
    string? ClientName,
    decimal? BudgetHours = null,
    bool IsBillable = false,
    IReadOnlyList<Guid>? MemberIds = null
) : IRequest<Result<Guid>>;
