using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Reports;

/// <summary>Whose hours a report may include: an employer sees everyone, others themself and their team.</summary>
public static class ReportScope {
    public static async Task<Result<IReadOnlyList<User>>> ResolveAsync(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser, Guid? userId, CancellationToken cancellationToken
    ) {
        IReadOnlyList<User> scopeUsers;

        if (currentUser.Role == UserRole.Employer) {
            scopeUsers = await unitOfWork.Users.GetAllAsync(cancellationToken);
        }
        else {
            var supervisees = await unitOfWork.Users.GetSupervisedByAsync(currentUser.UserId, cancellationToken);
            var self = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
            scopeUsers = self is null ? supervisees : supervisees.Append(self).ToList();

            if (scopeUsers.All(u => u.Id != currentUser.UserId) && userId != currentUser.UserId) {
                return Result<IReadOnlyList<User>>.Failure(
                    "You do not have the authority to view this report.", ResultErrorType.Forbidden);
            }
        }

        if (userId.HasValue) {
            if (scopeUsers.All(u => u.Id != userId.Value)) {
                return Result<IReadOnlyList<User>>.Failure(
                    "You do not have the authority to view this employee's report.", ResultErrorType.Forbidden);
            }

            scopeUsers = scopeUsers.Where(u => u.Id == userId.Value).ToList();
        }

        return Result<IReadOnlyList<User>>.Success(scopeUsers);
    }
}
