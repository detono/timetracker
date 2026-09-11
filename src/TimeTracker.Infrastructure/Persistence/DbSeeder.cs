using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence;

public static class DbSeeder {
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger
    ) {
        // Creates the schema directly from the current EF Core model - no hand-generated
        // migration files required, so there's nothing extra to run or forget after a
        // fresh `docker compose down -v`. Trade-off: this can't incrementally alter a
        // database that already has real data when the model changes later; it can only
        // create the schema on an empty database. If you outgrow that, switch to proper
        // migrations: `dotnet ef migrations add <Name> --project src/TimeTracker.Infrastructure
        // --startup-project src/TimeTracker.API --output-dir Persistence/Migrations`, commit
        // the generated folder, and change the line below to `await context.Database.MigrateAsync();`.
        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync()) {
            return;
        }

        var email = configuration["Seed:EmployerEmail"];
        var password = configuration["Seed:EmployerPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
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