using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// On an empty database, creates exactly one bootstrap Employer account from configuration
/// (never hardcoded demo data) - since account creation is Employer-only, something has to
/// exist to log in with the very first time. Configure via the "Seed" section, e.g. in
/// docker-compose.yml as Seed__EmployerEmail / Seed__EmployerPassword (see .env.example).
/// If nothing is configured, the database is left empty and a warning is logged.
///
/// Also seeds a small set of default hour types (Work, Sick Leave, PTO) if none exist yet,
/// regardless of whether a bootstrap employer is configured - the employer can rename,
/// recolor, deactivate, or add more of these later from "Hour types".
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        // Creates the schema directly from the current EF Core model - no hand-generated
        // migration files required. Trade-off: this can only create the schema on an empty
        // database, it can't incrementally alter one that already has real data when the
        // model changes later. If you outgrow that, switch to proper migrations.
        await context.Database.EnsureCreatedAsync();

        await SeedHourTypesAsync(context, logger);
        await SeedBootstrapEmployerAsync(context, passwordHasher, configuration, logger);
    }

    private static async Task SeedHourTypesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.HourTypes.AnyAsync())
        {
            return;
        }

        var defaults = new[]
        {
            HourType.Create("Work", "#932e4a"),
            HourType.Create("Sick Leave", "#b3452f"),
            HourType.Create("PTO", "#2f6f62"),
            HourType.Create("ADV", "#7a6a9e")
        };

        context.HourTypes.AddRange(defaults);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} default hour types.", defaults.Length);
    }

    private static async Task SeedBootstrapEmployerAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var email = configuration["Seed:EmployerEmail"];
        var password = configuration["Seed:EmployerPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No users exist and no bootstrap employer is configured (Seed:EmployerEmail / " +
                "Seed:EmployerPassword). The database is empty and nobody will be able to log in " +
                "until you set those values (see .env.example) and restart.");
            return;
        }

        var firstName = configuration["Seed:EmployerFirstName"];
        var lastName = configuration["Seed:EmployerLastName"];

        var employer = User.Create(
            string.IsNullOrWhiteSpace(firstName) ? "Admin" : firstName,
            string.IsNullOrWhiteSpace(lastName) ? "Employer" : lastName,
            email,
            passwordHasher.Hash(password),
            UserRole.Employer);

        context.Users.Add(employer);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded bootstrap employer account {Email}.", email);
    }
}
