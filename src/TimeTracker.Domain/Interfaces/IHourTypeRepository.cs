using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IHourTypeRepository
{
    Task<HourType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HourType>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(HourType hourType, CancellationToken cancellationToken = default);
    void Update(HourType hourType);
}
