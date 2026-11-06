using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// Lets a device (the mobile app) get a new sign-in token without asking for the password
/// again. Only a hash is stored. Each one works once: using it hands out a new one in the
/// same <see cref="FamilyId"/> (one family per sign-in on a device). If a used one comes
/// back later, someone copied it, and the whole family is revoked.
/// </summary>
/// <remarks>
/// Not tenant-owned on purpose: it's used before anyone is signed in (so there's no company
/// yet to filter on) and platform admins, who have no company, get them too.
/// </remarks>
public class RefreshToken : BaseEntity {
    /// <summary>
    /// How long a just-used token may be used once more. Covers a phone that sent the refresh
    /// but lost the answer on a bad connection and retries, without that looking like theft.
    /// </summary>
    public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public Guid UserId { get; private set; }

    /// <summary>The user's company when issued, to look the user up inside it. Null for platform admins.</summary>
    public Guid? TenantId { get; private set; }

    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = default!;

    /// <summary>The user's <see cref="User.SessionVersion"/> when issued; a password change or reset ends it.</summary>
    public int SessionVersion { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>When it was exchanged for a new one.</summary>
    public DateTime? UsedAtUtc { get; private set; }

    /// <summary>When it was revoked: signed out, or its family was caught being reused.</summary>
    public DateTime? RevokedAtUtc { get; private set; }

    private RefreshToken() {
        // EF Core
    }

    public static RefreshToken Issue(User user, string tokenHash, Guid familyId, DateTime nowUtc, TimeSpan lifetime) {
        if (string.IsNullOrWhiteSpace(tokenHash)) {
            throw new DomainException("A token hash is required.");
        }

        if (familyId == Guid.Empty) {
            throw new DomainException("A token family is required.");
        }

        if (lifetime <= TimeSpan.Zero) {
            throw new DomainException("A refresh token must have a positive lifetime.");
        }

        return new RefreshToken {
            UserId = user.Id,
            TenantId = user.TenantId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            SessionVersion = user.SessionVersion,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc + lifetime
        };
    }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>Unused, or used only moments ago (see <see cref="ReuseGracePeriod"/>), and not revoked or expired.</summary>
    public bool CanBeExchanged(DateTime nowUtc) =>
        !IsRevoked && !IsExpired(nowUtc) && (UsedAtUtc is not { } used || nowUtc - used <= ReuseGracePeriod);

    /// <summary>A used token coming back after the grace period: a sign that it was copied.</summary>
    public bool IsReplay(DateTime nowUtc) => UsedAtUtc is { } used && nowUtc - used > ReuseGracePeriod;

    public void MarkUsed(DateTime nowUtc) {
        if (!CanBeExchanged(nowUtc)) {
            throw new DomainException("This refresh token can no longer be used.");
        }

        UsedAtUtc ??= nowUtc;
        MarkUpdated();
    }

    public void Revoke(DateTime nowUtc) {
        if (IsRevoked) {
            return;
        }

        RevokedAtUtc = nowUtc;
        MarkUpdated();
    }
}
