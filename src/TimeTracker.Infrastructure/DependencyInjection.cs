using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Infrastructure.Email;
using TimeTracker.Infrastructure.Holidays;
using TimeTracker.Infrastructure.Identity;
using TimeTracker.Infrastructure.Persistence;
using TimeTracker.Infrastructure.Persistence.Repositories;
using TimeTracker.Infrastructure.Services;

namespace TimeTracker.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<IHourTypeRepository, HourTypeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantMailSettingsRepository, TenantMailSettingsRepository>();
        services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
        services.AddScoped<IPublicHolidayRepository, PublicHolidayRepository>();
        services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();
        services.AddScoped<ILockedMonthRepository, LockedMonthRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionValidator, SessionValidator>();
        services.AddSingleton<ILoginThrottle, MemoryLoginThrottle>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Keys live in the database so encrypted mail secrets stay readable across container restarts.
        services.AddDataProtection()
            .SetApplicationName("TimeTracker")
            .PersistKeysToDbContext<ApplicationDbContext>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

        services.AddSingleton<IGraphTokenProvider, ClientSecretGraphTokenProvider>();
        services.AddHttpClient<GraphMailTransport>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<SmtpMailTransport>();
        services.AddScoped<IMailDispatcher, MailDispatcher>();
        services.AddScoped<IEmailSender, EmailSender>();

        var holidayBaseUrl = configuration["PublicHolidays:BaseUrl"];
        services.AddHttpClient<IPublicHolidayProvider, NagerDatePublicHolidayProvider>(client => {
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(holidayBaseUrl)
                ? NagerDatePublicHolidayProvider.DefaultBaseUrl
                : holidayBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}