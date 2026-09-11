using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.API.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (auth, middleware, controllers) against an in-memory
/// EF Core database so integration tests don't require a running Postgres instance.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide a bootstrap employer for tests, the same way a real deployment would
        // via environment variables - nothing here is hardcoded into the app itself.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:EmployerEmail"] = "employer@demo.local",
                ["Seed:EmployerPassword"] = "Password123!",
                ["Seed:EmployerFirstName"] = "Test",
                ["Seed:EmployerLastName"] = "Employer"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TimeTrackerTests-{Guid.NewGuid()}"));
        });
    }
}
