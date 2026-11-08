using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Projects.Commands.UpdateProject;

/// <param name="MemberIds">Who may log hours on it. Empty or left out means everyone.</param>
public record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? ClientName,
    decimal? BudgetHours = null,
    bool IsBillable = false,
    IReadOnlyList<Guid>? MemberIds = null
) : IRequest<Result>;
