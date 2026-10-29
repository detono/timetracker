using Microsoft.Extensions.Caching.Memory;
using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.Infrastructure.Identity;

/// <summary>
/// Keeps failed sign-ins in memory: 5 wrong passwords within 15 minutes block the address for
/// 15 minutes. Memory is enough for a single server; a restart forgets the counts, which only
/// gives an attacker a fresh 5 tries. The number of tracked addresses is capped, so
/// guessing with random addresses can't fill the memory.
/// </summary>
public sealed class MemoryLoginThrottle(IDateTimeProvider clock) : ILoginThrottle, IDisposable {
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);
    private const int MaxTrackedAddresses = 100_000;

    private sealed class State {
        public int Failures;
        public DateTime WindowStartUtc;
        public DateTime? LockedUntilUtc;
    }

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxTrackedAddresses });
    private readonly object _gate = new();

    public TimeSpan? RemainingLockout(string email) {
        lock (_gate) {
            if (!_cache.TryGetValue(Key(email), out State? state) || state?.LockedUntilUtc is not { } until) {
                return null;
            }

            var remaining = until - clock.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
    }

    public void RecordFailure(string email) {
        lock (_gate) {
            var now = clock.UtcNow;
            var key = Key(email);
            if (!_cache.TryGetValue(key, out State? state) || state is null || now - state.WindowStartUtc > Window) {
                state = new State { WindowStartUtc = now };
            }

            state.Failures++;
            if (state.Failures >= MaxFailures) {
                state.LockedUntilUtc = now + Lockout;
                // Start counting afresh once the lockout ends.
                state.Failures = 0;
                state.WindowStartUtc = now + Lockout;
            }

            _cache.Set(key, state, new MemoryCacheEntryOptions {
                Size = 1,
                AbsoluteExpirationRelativeToNow = Window + Lockout
            });
        }
    }

    public void Reset(string email) {
        lock (_gate) {
            _cache.Remove(Key(email));
        }
    }

    public void Dispose() => _cache.Dispose();

    private static string Key(string email) => email.Trim().ToLowerInvariant();
}
