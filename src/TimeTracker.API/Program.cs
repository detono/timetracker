using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TimeTracker.API.Authorization;
using TimeTracker.API.Converters;
using TimeTracker.API.Middleware;
using TimeTracker.API.Services;
using TimeTracker.Application;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Security;
using TimeTracker.Infrastructure;
using TimeTracker.Infrastructure.Identity;
using TimeTracker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

// ---- Layered composition: each layer exposes its own AddXyz() extension (Dependency Inversion) ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
builder.Services.AddScoped<ITenantScope>(sp => sp.GetRequiredService<CurrentUserService>());

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new TolerantTimeOnlyConverter());
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options => {
    options.AddPolicy("Frontend", policy => {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TimeTracker API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter a valid JWT bearer token."
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
                { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(options => {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Deactivating someone (or their company) or resetting their password signs them out
        // on their next request, not only when the token expires.
        options.Events = new JwtBearerEvents {
            OnTokenValidated = async context => {
                var principal = context.Principal!;
                if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) {
                    context.Fail("The token has no user.");
                    return;
                }

                // Tokens from before session versions existed count as version 0.
                var version = int.TryParse(principal.FindFirstValue(SessionClaimTypes.SessionVersion), out var v) ? v : 0;
                var validator = context.HttpContext.RequestServices.GetRequiredService<ISessionValidator>();
                if (!await validator.IsValidAsync(userId, version, context.HttpContext.RequestAborted)) {
                    context.Fail("This sign-in is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options => options.AddTenantPolicies());
builder.Services.AddPrivateNetworkProxies();
builder.Services.AddAuthRateLimiting();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Apply migrations and seed a bootstrap employer (from configuration only) on startup.
using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var seedLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    
    try {
        await dbContext.Database.MigrateAsync();
    } catch (Exception ex) {
        seedLogger.LogError(ex, "An error occurred migrating the database.");  
    }

    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    
    await DbSeeder.SeedAsync(dbContext, passwordHasher, app.Configuration, seedLogger);
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }