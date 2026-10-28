using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.API.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (auth, middleware, controllers) against an in-memory
/// EF Core database so integration tests don't require a running Postgres instance.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program> {
    public const string AdminEmail = "admin@demo.local";
    public const string AdminPassword = "Password123!";

    private readonly string _dbName = $"TimeTrackerTests-{Guid.NewGuid()}";
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Testing");

        // Provide a bootstrap platform admin for tests, the same way a real deployment would
        // via environment variables - nothing here is hardcoded into the app itself.
        builder.ConfigureAppConfiguration((_, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
                ["Seed:AdminFirstName"] = "Test",
                ["Seed:AdminLastName"] = "Admin"
            });
        });

        builder.ConfigureServices(services => {
            var descriptor = services.SingleOrDefault(d => 
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
            );
            
            if (descriptor is not null) {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
            );

            // Never call the real public holiday service from tests.
            services.RemoveAll<IPublicHolidayProvider>();
            services.AddSingleton<IPublicHolidayProvider, FakePublicHolidayProvider>();
            
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IPasswordHasher>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomWebApplicationFactory>>();
            
            DbSeeder.SeedAsync(context, passwordHasher, config, logger).GetAwaiter().GetResult();
        });
    }
}

/// <summary>Belgian-style holidays for any year, plus one regional German holiday; "XX" is unknown.</summary>
public class FakePublicHolidayProvider : IPublicHolidayProvider {
    public Task<IReadOnlyList<ExternalPublicHoliday>> GetAsync(string countryCode, int year, CancellationToken cancellationToken = default) {
        if (countryCode == "XX") {
            throw new PublicHolidayLookupException("No public holidays are known for country XX. Add them by hand.");
        }

        IReadOnlyList<ExternalPublicHoliday> holidays = new[] {
            new ExternalPublicHoliday(new DateOnly(year, 11, 1), "Allerheiligen", null),
            new ExternalPublicHoliday(new DateOnly(year, 11, 11), "Wapenstilstand", null),
            new ExternalPublicHoliday(new DateOnly(year, 1, 6), "Heilige Drei Könige", new[] { "DE-BY" })
        };
        return Task.FromResult(holidays);
    }
}
