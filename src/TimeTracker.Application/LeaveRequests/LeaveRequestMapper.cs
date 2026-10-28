using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Leave;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LeaveRequests;

/// <summary>Turns leave requests into DTOs, including what the caller may do and the balance warning.</summary>
public class LeaveRequestMapper(IUnitOfWork unitOfWork, ICurrentUserService currentUser, LeaveService leave) {
    public async Task<IReadOnlyList<LeaveRequestDto>> ToDtosAsync(
        IReadOnlyList<LeaveRequest> requests, CancellationToken cancellationToken
    ) {
        if (requests.Count == 0) {
            return Array.Empty<LeaveRequestDto>();
        }

        var users = (await unitOfWork.Users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
        var hourTypes = (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
            .ToDictionary(t => t.Id);

        var dtos = new List<LeaveRequestDto>(requests.Count);
        foreach (var request in requests) {
            dtos.Add(await ToDtoAsync(request, users, hourTypes, cancellationToken));
        }

        return dtos;
    }

    public async Task<LeaveRequestDto> ToDtoAsync(LeaveRequest request, CancellationToken cancellationToken) =>
        (await ToDtosAsync(new[] { request }, cancellationToken))[0];

    private async Task<LeaveRequestDto> ToDtoAsync(
        LeaveRequest request,
        IReadOnlyDictionary<Guid, User> users,
        IReadOnlyDictionary<Guid, HourType> hourTypes,
        CancellationToken cancellationToken
    ) {
        users.TryGetValue(request.UserId, out var requester);
        hourTypes.TryGetValue(request.HourTypeId, out var hourType);
        User? reviewer = null;
        if (request.ReviewedById is { } reviewerId) {
            users.TryGetValue(reviewerId, out reviewer);
        }

        var canReview = requester is not null && leave.CanReview(requester);

        decimal? remaining = null;
        var exceeds = false;
        if (request.Status == LeaveRequestStatus.Pending && hourType is not null) {
            var schedule = await leave.GetScheduleAsync(request.UserId, cancellationToken);
            var check = await leave.CheckBalanceAsync(request, hourType, schedule, cancellationToken);
            remaining = check.Balance?.RemainingHours;
            exceeds = check.Exceeds;
        }

        return new LeaveRequestDto(
            request.Id,
            request.UserId,
            requester is null ? "Unknown" : $"{requester.FirstName} {requester.LastName}",
            request.HourTypeId,
            hourType?.LocalizedNames ?? new Dictionary<string, string>(),
            hourType?.ColorHex ?? "#999999",
            request.StartDate,
            request.EndDate,
            request.Hours,
            request.Comment,
            request.Status.ToString(),
            reviewer is null ? null : $"{reviewer.FirstName} {reviewer.LastName}",
            request.ReviewedAtUtc,
            request.ReviewComment,
            request.CreatedAtUtc,
            canReview,
            CanCancel(request, canReview),
            remaining,
            exceeds);
    }

    /// <summary>The requester can withdraw a pending request; a reviewer can also revoke an approved one.</summary>
    public bool CanCancel(LeaveRequest request, bool canReview) => request.Status switch {
        LeaveRequestStatus.Pending => canReview || request.UserId == currentUser.UserId,
        LeaveRequestStatus.Approved => canReview,
        _ => false
    };

    public static string NameOf(HourType hourType) =>
        hourType.LocalizedNames.TryGetValue("en", out var en) ? en : hourType.LocalizedNames.Values.FirstOrDefault() ?? "Leave";
}
