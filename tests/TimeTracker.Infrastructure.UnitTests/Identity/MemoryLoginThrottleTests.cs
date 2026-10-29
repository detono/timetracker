using FluentAssertions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Infrastructure.Identity;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Identity;

public class MemoryLoginThrottleTests {
    private sealed class FakeClock : IDateTimeProvider {
        public DateTime UtcNow { get; set; } = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);
        public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);
    }

    private readonly FakeClock _clock = new();

    [Fact]
    public void FifthWrongPassword_BlocksTheAddressFor15Minutes() {
        using var throttle = new MemoryLoginThrottle(_clock);

        for (var i = 0; i < MemoryLoginThrottle.MaxFailures - 1; i++) throttle.RecordFailure("eva@company.test");
        throttle.RemainingLockout("eva@company.test").Should().BeNull();

        throttle.RecordFailure("EVA@company.test ");
        throttle.RemainingLockout("eva@company.test").Should().Be(TimeSpan.FromMinutes(15));

        _clock.UtcNow = _clock.UtcNow.AddMinutes(15);
        throttle.RemainingLockout("eva@company.test").Should().BeNull();
    }

    [Fact]
    public void FailuresSpreadOverMoreThan15Minutes_DontAddUp() {
        using var throttle = new MemoryLoginThrottle(_clock);

        for (var i = 0; i < MemoryLoginThrottle.MaxFailures - 1; i++) throttle.RecordFailure("eva@company.test");
        _clock.UtcNow = _clock.UtcNow.AddMinutes(16);
        throttle.RecordFailure("eva@company.test");

        throttle.RemainingLockout("eva@company.test").Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsTheCount_AndOtherAddressesAreUnaffected() {
        using var throttle = new MemoryLoginThrottle(_clock);
        for (var i = 0; i < MemoryLoginThrottle.MaxFailures; i++) throttle.RecordFailure("eva@company.test");

        throttle.RemainingLockout("bea@company.test").Should().BeNull();
        throttle.Reset("eva@company.test");
        throttle.RemainingLockout("eva@company.test").Should().BeNull();
    }
}
