namespace TimeTracker.Domain.Interfaces;

/// <summary>
/// Coordinates the persistence of changes across repositories within a single transaction/context.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ITimeEntryRepository TimeEntries { get; }
    IHourTypeRepository HourTypes { get; }
    IProjectRepository Projects { get; }
    ITenantRepository Tenants { get; }
    ITenantMailSettingsRepository MailSettings { get; }
    IWorkScheduleRepository WorkSchedules { get; }
    ILeaveRequestRepository LeaveRequests { get; }
    ILeaveBalanceRepository LeaveBalances { get; }
    IPublicHolidayRepository PublicHolidays { get; }
    IPasswordResetCodeRepository PasswordResetCodes { get; }
    ILockedMonthRepository LockedMonths { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
