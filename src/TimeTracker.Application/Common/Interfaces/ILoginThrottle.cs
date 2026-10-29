namespace TimeTracker.Application.Common.Interfaces;

/// <summary>
/// Slows down password guessing per account: after too many wrong passwords for one email
/// address, signing in with it is blocked for a while. Works the same for addresses without
/// an account, so it doesn't reveal which ones exist.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>How long signing in with this email is still blocked, or null when it isn't.</summary>
    TimeSpan? RemainingLockout(string email);

    void RecordFailure(string email);

    /// <summary>After a successful sign-in or password reset.</summary>
    void Reset(string email);
}
