using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class ProjectRepository(ApplicationDbContext context) : IProjectRepository {
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await context.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Project>> GetAllAsync(bool includeInactive = false,
        CancellationToken cancellationToken = default) {
        var query = context.Projects.AsQueryable();

        if (!includeInactive) {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default) {
        await context.Projects.AddAsync(project, cancellationToken);
    }

    public void Update(Project project) {
        context.Projects.Update(project);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        return await context.SaveChangesAsync(cancellationToken);
    }
}