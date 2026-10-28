using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveBalances.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveBalances.Queries.GetLeaveBalances;

public class GetLeaveBalancesQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    LeaveService leave
) : IRequestHandler<GetLeaveBalancesQuery, Result<IReadOnlyList<LeaveBalanceDto>>> {
    public async Task<Result<IReadOnlyList<LeaveBalanceDto>>> Handle(
        GetLeaveBalancesQuery request, CancellationToken cancellationToken
    ) {
        if (request.Year is < 2000 or > 2100) {
            return Result<IReadOnlyList<LeaveBalanceDto>>.Failure("The year is out of range.");
        }

        IReadOnlyList<User> users;
        if (currentUser.Role == UserRole.Employer) {
            users = (await unitOfWork.Users.GetAllAsync(cancellationToken))
                .Where(u => u.IsActive && u.Role == UserRole.Employee)
                .ToList();
        } else {
            var self = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
            var supervisees = await unitOfWork.Users.GetSupervisedByAsync(currentUser.UserId, cancellationToken);
            users = (self is null ? supervisees : supervisees.Prepend(self)).Where(u => u.IsActive).ToList();
        }

        var userIds = users.Select(u => u.Id).ToList();
        var from = new DateOnly(request.Year, 1, 1);
        var to = new DateOnly(request.Year, 12, 31);

        var deducting = await leave.DeductingHourTypeIdsAsync(cancellationToken);
        var allowances = (await unitOfWork.LeaveBalances.GetForYearAsync(request.Year, cancellationToken))
            .ToDictionary(b => b.UserId);
        var entriesByUser = (await unitOfWork.TimeEntries.GetForUsersAsync(userIds, from, to, cancellationToken))
            .ToLookup(e => e.UserId);
        var pendingByUser = (await unitOfWork.LeaveRequests.GetAsync(userIds, LeaveRequestStatus.Pending, cancellationToken))
            .Where(r => deducting.Contains(r.HourTypeId) && r.Overlaps(from, to))
            .ToLookup(r => r.UserId);

        var holidays = await leave.GetHolidaysAsync(from, to, cancellationToken);
        var dtos = new List<LeaveBalanceDto>(users.Count);
        foreach (var user in users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)) {
            var used = LeaveService.UsedHours(entriesByUser[user.Id], deducting);

            decimal pending = 0;
            var userPending = pendingByUser[user.Id].ToList();
            if (userPending.Count > 0) {
                var schedule = await leave.GetScheduleAsync(user.Id, cancellationToken);
                pending = userPending.Sum(r => LeaveService.HoursInYear(r, schedule, request.Year, holidays));
            }

            decimal? allowance = allowances.TryGetValue(user.Id, out var balance) ? balance.AllowanceHours : null;
            dtos.Add(new LeaveBalanceDto(
                user.Id, $"{user.FirstName} {user.LastName}", request.Year, allowance, used, pending, allowance - used));
        }

        return Result<IReadOnlyList<LeaveBalanceDto>>.Success(dtos);
    }
}
