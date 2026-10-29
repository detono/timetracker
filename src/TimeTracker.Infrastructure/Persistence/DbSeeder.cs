using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// When no platform admin exists yet, creates exactly one bootstrap platform admin from
/// configuration (never hardcoded demo data) - since tenants and their first employer are
/// created by a platform admin, something has to exist to log in with the very first time.
/// Configure via the "Seed" section, e.g. in docker-compose.yml as Seed__AdminEmail /
/// Seed__AdminPassword (see .env.example). If nothing is configured, no account is created
/// and a warning is logged.
///
/// Default hour types are not seeded here: each tenant gets its own set when it is created.
/// </summary>
public static class DbSeeder {
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger
    ) {
        if (context.Database.IsRelational()) {
            await context.Database.MigrateAsync();
        } else {
            // Fallback for the In-Memory database provider used in integration tests
            await context.Database.EnsureCreatedAsync();
        }

        await SeedBootstrapPlatformAdminAsync(context, passwordHasher, configuration, logger);
    }

    /// <summary>SEED_ADMIN_PASSWORD from .env.example.</summary>
    public const string ExamplePassword = "change_me_then_change_it_again_after_first_login";

    private static async Task SeedBootstrapPlatformAdminAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger
    ) {
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.PlatformAdmin)) {
            return;
        }

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            logger.LogWarning(
                "No platform admin exists and no bootstrap admin is configured (Seed:AdminEmail / " +
                "Seed:AdminPassword). Nobody will be able to create tenants until you set those values " +
                "(see .env.example) and restart.");
            return;
        }

        // The example password is public: refuse it, so nobody can sign in with it before you do.
        if (password.Trim() == ExamplePassword) {
            logger.LogError(
                "Not creating the bootstrap platform admin: Seed:AdminPassword (SEED_ADMIN_PASSWORD) is still the " +
                "example value from .env.example. Set your own password and restart.");
            return;
        }

        var firstName = configuration["Seed:AdminFirstName"];
        var lastName = configuration["Seed:AdminLastName"];

        var admin = User.CreatePlatformAdmin(
            string.IsNullOrWhiteSpace(firstName) ? "Platform" : firstName,
            string.IsNullOrWhiteSpace(lastName) ? "Admin" : lastName,
            email,
            passwordHasher.Hash(password));

        // The password sits in configuration in plain text, so it's only good for the first login.
        admin.RequirePasswordChange();
        context.Users.Add(admin);
        var seedAmount = await context.SaveChangesAsync();

        logger.LogInformation("Seeded bootstrap platform admin account {Email}. Success? {Success}", email, seedAmount == 1);
    }
}
