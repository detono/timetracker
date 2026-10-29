using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace TimeTracker.API.Authorization;

/// <summary>
/// Limits how fast one IP address can try passwords or reset codes, with ASP.NET Core's
/// built-in rate limiter. The per-account lockout (ILoginThrottle) covers guessing one
/// account from many addresses; this covers trying many accounts from one address.
/// </summary>
public static class RateLimitPolicies {
    public const string Auth = "auth";

    /// <summary>Requests per minute per IP address on sign-in and password reset. Configurable for busy offices behind one IP.</summary>
    public const int DefaultAuthRequestsPerMinute = 20;

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services) {
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) => {
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many attempts from your network. Wait a minute and try again." }, cancellationToken);
            };
            options.AddPolicy(Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions {
                    // Read per address when it's first seen, so configuration added late (e.g. by tests) counts.
                    PermitLimit = httpContext.RequestServices.GetRequiredService<IConfiguration>()
                        .GetValue("RateLimiting:AuthRequestsPerMinute", DefaultAuthRequestsPerMinute),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }

    /// <summary>
    /// Behind the bundled nginx (or another reverse proxy) every request seems to come from the
    /// proxy. Trust X-Forwarded-For from proxies on private networks, so limits apply per real
    /// client. A request straight from the internet can't fake its address this way.
    /// </summary>
    public static IServiceCollection AddPrivateNetworkProxies(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            options.ForwardLimit = null;
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("fc00::"), 7));
        });
}
