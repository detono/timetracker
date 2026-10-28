using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests.Queries.GetPendingLeaveCount;

public class GetPendingLeaveCountQueryHandler(
    IUnitOfWork unitOfWork,
    LeaveService leave
) : IRequestHandler<GetPendingLeaveCountQuery, Result<int>> {
    public async Task<Result<int>> Handle(GetPendingLeaveCountQuery request, CancellationToken cancellationToken) {
        var userIds = await leave.ReviewableUserIdsAsync(cancellationToken);
        if (userIds is { Count: 0 }) {
            return Result<int>.Success(0);
        }

        return Result<int>.Success(await unitOfWork.LeaveRequests.CountPendingAsync(userIds, cancellationToken));
    }
}
