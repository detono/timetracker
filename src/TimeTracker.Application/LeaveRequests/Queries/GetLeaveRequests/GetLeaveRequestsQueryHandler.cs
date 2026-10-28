using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests.Queries.GetLeaveRequests;

public class GetLeaveRequestsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    LeaveService leave,
    LeaveRequestMapper mapper
) : IRequestHandler<GetLeaveRequestsQuery, Result<IReadOnlyList<LeaveRequestDto>>> {
    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(
        GetLeaveRequestsQuery request, CancellationToken cancellationToken
    ) {
        IReadOnlyList<Guid>? userIds = request.Scope == LeaveRequestScope.Mine
            ? new[] { currentUser.UserId }
            : await leave.ReviewableUserIdsAsync(cancellationToken);

        if (userIds is { Count: 0 }) {
            return Result<IReadOnlyList<LeaveRequestDto>>.Success(Array.Empty<LeaveRequestDto>());
        }

        var requests = await unitOfWork.LeaveRequests.GetAsync(userIds, request.Status, cancellationToken);
        return Result<IReadOnlyList<LeaveRequestDto>>.Success(await mapper.ToDtosAsync(requests, cancellationToken));
    }
}
