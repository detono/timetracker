using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IProjectRepository {
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Project>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Update(Project project);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}