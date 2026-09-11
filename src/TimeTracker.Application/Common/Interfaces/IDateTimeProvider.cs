namespace TimeTracker.Application.Common.Interfaces;

/// <summary>Abstraction over the system clock so handlers stay unit-testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly TodayUtc { get; }
}
