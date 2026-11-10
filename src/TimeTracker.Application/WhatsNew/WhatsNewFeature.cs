using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.WhatsNew;

public record ReleaseSectionDto(string Title, IReadOnlyList<string> Items);

public record ReleaseDto(string Version, DateOnly Date, IReadOnlyList<ReleaseSectionDto> Sections) {
    public static ReleaseDto From(Release r) =>
        new(r.Version, r.Date, r.Sections.Select(s => new ReleaseSectionDto(s.Title, s.Items)).ToList());
}

/// <summary>The releases the caller hasn't seen yet, newest first. Empty when there's nothing new.</summary>
public record WhatsNewDto(IReadOnlyList<ReleaseDto> Releases);

/// <summary>
/// What changed since the caller last looked. Someone who closed the notes before gets every
/// newer release (at most <see cref="GetWhatsNewQueryHandler.MaxReleases"/>). Someone who never
/// did gets only the newest release, and only if their account is older than it, so a new
/// colleague isn't greeted with history.
/// </summary>
public record GetWhatsNewQuery : IRequest<Result<WhatsNewDto>>;

/// <summary>Records that the caller saw the notes up to this release, so they don't show again.</summary>
public record MarkWhatsNewSeenCommand(string Version) : IRequest<Result>;

public class GetWhatsNewQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ReleaseNotes notes
) : IRequestHandler<GetWhatsNewQuery, Result<WhatsNewDto>> {
    public const int MaxReleases = 10;

    public async Task<Result<WhatsNewDto>> Handle(GetWhatsNewQuery request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null) {
            return Result<WhatsNewDto>.Failure("Your account was not found.", ResultErrorType.NotFound);
        }

        var releases = notes.Releases;
        if (releases.Count == 0) {
            return Result<WhatsNewDto>.Success(new WhatsNewDto([]));
        }

        IEnumerable<Release> unseen;
        if (ReleaseNotes.TryParseVersion(user.LastSeenVersion, out var lastSeen)) {
            unseen = releases.Where(r => r.Number > lastSeen).Take(MaxReleases);
        } else {
            var newest = releases[0];
            unseen = DateOnly.FromDateTime(user.CreatedAtUtc) < newest.Date ? [newest] : [];
        }

        return Result<WhatsNewDto>.Success(new WhatsNewDto(unseen.Select(ReleaseDto.From).ToList()));
    }
}

public class MarkWhatsNewSeenCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ReleaseNotes notes
) : IRequestHandler<MarkWhatsNewSeenCommand, Result> {
    public async Task<Result> Handle(MarkWhatsNewSeenCommand request, CancellationToken cancellationToken) {
        var release = ReleaseNotes.TryParseVersion(request.Version, out var number)
            ? notes.Releases.FirstOrDefault(r => r.Number == number)
            : null;
        if (release is null) {
            return Result.Failure($"There's no release {request.Version}.", ResultErrorType.NotFound);
        }

        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null) {
            return Result.Failure("Your account was not found.", ResultErrorType.NotFound);
        }

        // Never go back: notes closed in an old tab don't bring newer ones back.
        if (ReleaseNotes.TryParseVersion(user.LastSeenVersion, out var lastSeen) && lastSeen >= number) {
            return Result.Success();
        }

        user.MarkVersionSeen(release.Version);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
