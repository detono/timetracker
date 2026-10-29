using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IAuditEntryRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>The newest entries first, older than <paramref name="beforeUtc"/> when given.</summary>
    Task<IReadOnlyList<AuditEntry>> GetPageAsync(DateTime? beforeUtc, int take, CancellationToken cancellationToken = default);
}
