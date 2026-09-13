using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class HourTypeRepository(ApplicationDbContext context) : IHourTypeRepository {
    public Task<HourType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.HourTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HourType>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default
    ) {
        var query = context.HourTypes.AsNoTracking().AsQueryable();
        if (!includeInactive) {
            query = query.Where(t => t.IsActive);
        }
        
        var hourTypes = await query.ToListAsync(cancellationToken);
        
        return hourTypes
            .OrderBy(h => h.LocalizedNames.ContainsKey("en") 
                ? h.LocalizedNames["en"] 
                : "Unknown")
            .ToList();
    }

    public async Task<bool> NameExistsAsync(
        string name, 
        Guid? excludingId = null,
        CancellationToken cancellationToken = default
    ) {
        var normalized = name.Trim();

        var allTypes = await context.HourTypes.ToListAsync(cancellationToken);

        return allTypes.Any(h => 
            h.Id != excludingId && 
            h.LocalizedNames.Values.Any(v => v.Trim().ToLowerInvariant() == normalized)
        );
    }

    public async Task AddAsync(HourType hourType, CancellationToken cancellationToken = default) =>
        await context.HourTypes.AddAsync(hourType, cancellationToken);

    public void Update(HourType hourType) => context.HourTypes.Update(hourType);
}