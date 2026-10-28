using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Infrastructure.Email;
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

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
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

        return services;
    }
}