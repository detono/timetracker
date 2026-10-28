using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IPublicHolidayRepository {
    Task<PublicHoliday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The company's holidays from <paramref name="from"/> to <paramref name="to"/>, inclusive, by date.</summary>
    Task<IReadOnlyList<PublicHoliday>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task AddAsync(PublicHoliday holiday, CancellationToken cancellationToken = default);
    void Remove(PublicHoliday holiday);
}
