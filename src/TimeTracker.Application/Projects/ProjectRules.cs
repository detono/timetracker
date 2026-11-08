using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Projects;

/// <summary>Checks shared by creating and changing a project.</summary>
public static class ProjectRules {
    /// <summary>The members as ids, after checking they're all people in this company.</summary>
    public static async Task<Result<IReadOnlyList<Guid>>> ResolveMembersAsync(
        IUnitOfWork unitOfWork, IReadOnlyList<Guid>? memberIds, CancellationToken cancellationToken
    ) {
        var ids = (memberIds ?? Array.Empty<Guid>()).Distinct().ToList();
        if (ids.Count == 0) {
            return Result<IReadOnlyList<Guid>>.Success(ids);
        }

        // The users query only returns this company's people.
        var known = (await unitOfWork.Users.GetAllAsync(cancellationToken))
            .Where(u => u.Role != UserRole.PlatformAdmin)
            .Select(u => u.Id)
            .ToHashSet();

        return ids.All(known.Contains)
            ? Result<IReadOnlyList<Guid>>.Success(ids)
            : Result<IReadOnlyList<Guid>>.Failure("A project member was not found.", ResultErrorType.Validation);
    }
}
