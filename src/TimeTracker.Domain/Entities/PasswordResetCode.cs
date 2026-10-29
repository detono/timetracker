using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A short-lived code, emailed to someone who forgot their password, that lets them choose
/// a new one. Only a hash is stored. It stops working after <see cref="Lifetime"/> or after
/// <see cref="MaxFailedAttempts"/> wrong guesses.
/// </summary>
public class PasswordResetCode : TenantOwnedEntity {
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }

    private PasswordResetCode() {
        // EF Core
    }

    public static PasswordResetCode Create(Guid userId, string codeHash, DateTime nowUtc) {
        if (userId == Guid.Empty) {
            throw new DomainException("A user is required.");
        }

        if (string.IsNullOrWhiteSpace(codeHash)) {
            throw new DomainException("A code hash is required.");
        }

        return new PasswordResetCode {
            UserId = userId,
            CodeHash = codeHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc + Lifetime
        };
    }

    public bool IsUsable(DateTime nowUtc) => nowUtc < ExpiresAtUtc && FailedAttempts < MaxFailedAttempts;

    public void RecordFailedAttempt() {
        FailedAttempts++;
        MarkUpdated();
    }
}
