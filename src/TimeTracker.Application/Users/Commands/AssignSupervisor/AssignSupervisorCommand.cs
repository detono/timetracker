using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Users.Commands.AssignSupervisor;

/// <summary>Employer-only action: grants an Employee authority to view another Employee's hours.</summary>
public record AssignSupervisorCommand(Guid EmployeeId, Guid? SupervisorId) : IRequest<Result>;
