using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.PlatformAdmins.Commands.SetPlatformAdminActive;

public record SetPlatformAdminActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;
