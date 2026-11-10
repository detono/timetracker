using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.AssignSupervisor;

/// <summary>
/// Employer-only action: lets an Employee see another Employee's hours and, when
/// <see cref="ApprovesLeave"/>, approve their leave. Without it, a new supervisor approves leave
/// and the same supervisor keeps what was set.
/// </summary>
public record AssignSupervisorCommand(Guid EmployeeId, Guid? SupervisorId, bool? ApprovesLeave = null) : IRequest<Result>;
