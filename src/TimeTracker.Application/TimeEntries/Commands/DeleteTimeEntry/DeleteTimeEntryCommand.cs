using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.TimeEntries.Commands.DeleteTimeEntry;

public record DeleteTimeEntryCommand(Guid Id) : IRequest<Result>;
