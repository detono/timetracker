using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class HourTypeRepository : IHourTypeRepository
{
    private readonly ApplicationDbContext _context;

    public HourTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<HourType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.HourTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HourType>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _context.HourTypes.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        var query = _context.HourTypes.Where(t => t.Name == normalized);
        if (excludingId.HasValue)
        {
            query = query.Where(t => t.Id != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(HourType hourType, CancellationToken cancellationToken = default) =>
        await _context.HourTypes.AddAsync(hourType, cancellationToken);

    public void Update(HourType hourType) => _context.HourTypes.Update(hourType);
}
