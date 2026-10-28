using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class UnitOfWork(
    ApplicationDbContext context,
    IUserRepository users,
    ITimeEntryRepository timeEntries,
    IHourTypeRepository hourTypes,
    IProjectRepository projects,
    ITenantRepository tenants,
    ITenantMailSettingsRepository mailSettings,
    IWorkScheduleRepository workSchedules,
    ILeaveRequestRepository leaveRequests,
    ILeaveBalanceRepository leaveBalances
) : IUnitOfWork {
    public IUserRepository Users { get; } = users;
    public ITimeEntryRepository TimeEntries { get; } = timeEntries;
    public IHourTypeRepository HourTypes { get; } = hourTypes;
    public IProjectRepository Projects { get; } = projects;
    public ITenantRepository Tenants { get; } = tenants;
    public ITenantMailSettingsRepository MailSettings { get; } = mailSettings;
    public IWorkScheduleRepository WorkSchedules { get; } = workSchedules;
    public ILeaveRequestRepository LeaveRequests { get; } = leaveRequests;
    public ILeaveBalanceRepository LeaveBalances { get; } = leaveBalances;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}