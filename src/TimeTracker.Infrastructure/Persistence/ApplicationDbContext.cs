using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {
    public DbSet<User> Users => Set<User>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<HourType> HourTypes => Set<HourType>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}