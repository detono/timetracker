using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Webhooks;

/// <summary>A webhook without its secrets: only whether the extra header has a value.</summary>
public record WebhookDto(
    Guid Id, string Name, string Url, IReadOnlyList<string> Events, bool IsEnabled,
    string? HeaderName, bool HasHeaderValue, DateTime CreatedAtUtc) {
    public static WebhookDto From(Webhook w) =>
        new(w.Id, w.Name, w.Url, w.Events, w.IsEnabled, w.HeaderName, w.HeaderValueProtected is not null, w.CreatedAtUtc);
}

public record WebhookDeliveryDto(
    Guid Id, Guid WebhookId, string WebhookName, string EventType, string Summary, WebhookDeliveryStatus Status,
    int Attempts, int? LastStatusCode, string? LastError, DateTime CreatedAtUtc, DateTime? LastAttemptAtUtc,
    DateTime? NextAttemptAtUtc) {
    public static WebhookDeliveryDto From(WebhookDelivery d, string webhookName) =>
        new(d.Id, d.WebhookId, webhookName, d.EventType, d.Summary, d.Status, d.Attempts, d.LastStatusCode,
            d.LastError, d.CreatedAtUtc, d.LastAttemptAtUtc, d.NextAttemptAtUtc);
}

/// <summary>Everything the Webhooks tab shows.</summary>
public record WebhooksOverviewDto(
    IReadOnlyList<string> AvailableEvents,
    IReadOnlyList<WebhookDto> Webhooks,
    IReadOnlyList<WebhookDeliveryDto> RecentDeliveries);

public record WebhookSecretDto(string Secret);

/// <summary>Employer only: the company's webhooks, the events they can use, and the latest calls.</summary>
public record GetWebhooksQuery : IRequest<Result<WebhooksOverviewDto>>;

/// <summary>
/// Employer only: creates a webhook (no <see cref="Id"/>) or changes one. A new webhook gets
/// a random signing secret. Leave <see cref="HeaderValue"/> empty to keep the stored value;
/// leave <see cref="HeaderName"/> empty to send no extra header.
/// </summary>
public record SaveWebhookCommand(
    Guid? Id, string Name, string Url, IReadOnlyList<string> Events, bool IsEnabled,
    string? HeaderName, string? HeaderValue) : IRequest<Result<WebhookDto>>;

/// <summary>Employer only: removes a webhook and its history.</summary>
public record DeleteWebhookCommand(Guid Id) : IRequest<Result>;

/// <summary>Employer only: the signing secret, to set up the receiving side.</summary>
public record GetWebhookSecretQuery(Guid Id) : IRequest<Result<WebhookSecretDto>>;

/// <summary>Employer only: replaces the signing secret; the old one stops working right away.</summary>
public record RotateWebhookSecretCommand(Guid Id) : IRequest<Result<WebhookSecretDto>>;

/// <summary>Employer only: sends a made-up event now (also while switched off) and returns how it went.</summary>
public record TestWebhookCommand(Guid Id) : IRequest<Result<WebhookDeliveryDto>>;

/// <summary>Employer only: "Send again" — queues a call for right away, with a fresh set of retries.</summary>
public record RetryWebhookDeliveryCommand(Guid Id) : IRequest<Result>;

public partial class SaveWebhookCommandValidator : AbstractValidator<SaveWebhookCommand> {
    public const int MaxHeaderValueLength = 4096;

    // Headers the app sets itself, or that would break the request.
    private static readonly HashSet<string> ReservedHeaders = new(StringComparer.OrdinalIgnoreCase) {
        "Host", "Content-Type", "Content-Length", "Content-Encoding", "Transfer-Encoding", "Connection",
        "Expect", "Upgrade", "TE", "Trailer", "Keep-Alive", "Proxy-Authorization", "Cookie"
    };

    public SaveWebhookCommandValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Webhook.MaxNameLength);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(Webhook.MaxUrlLength)
            .Must(url => Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                         && string.IsNullOrEmpty(uri.UserInfo))
            .WithMessage("The URL must start with https:// or http://, without a user name or password in it.");
        RuleFor(x => x.Events).NotEmpty().WithMessage("Choose at least one event.");
        RuleForEach(x => x.Events).Must(e => WebhookEvents.All.Contains(e)).WithMessage("'{PropertyValue}' isn't an event.");

        When(x => !string.IsNullOrWhiteSpace(x.HeaderName), () => {
            RuleFor(x => x.HeaderName!).MaximumLength(Webhook.MaxHeaderNameLength)
                .Must(n => HeaderName().IsMatch(n.Trim())).WithMessage("A header name can only have letters, digits and dashes.")
                .Must(n => !ReservedHeaders.Contains(n.Trim()) && !n.Trim().StartsWith("X-Webhook-", StringComparison.OrdinalIgnoreCase))
                .WithMessage("This header is set by the app itself; choose another.");
        });
        RuleFor(x => x.HeaderValue).MaximumLength(MaxHeaderValueLength)
            .Must(v => v is null || !v.Contains('\r') && !v.Contains('\n')).WithMessage("The header value must be on one line.");
    }

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex HeaderName();
}

public class WebhooksHandlers(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ISecretProtector secretProtector,
    IDateTimeProvider clock,
    WebhookDeliveryProcessor processor,
    AuditTrail audit
) : IRequestHandler<GetWebhooksQuery, Result<WebhooksOverviewDto>>,
    IRequestHandler<SaveWebhookCommand, Result<WebhookDto>>,
    IRequestHandler<DeleteWebhookCommand, Result>,
    IRequestHandler<GetWebhookSecretQuery, Result<WebhookSecretDto>>,
    IRequestHandler<RotateWebhookSecretCommand, Result<WebhookSecretDto>>,
    IRequestHandler<TestWebhookCommand, Result<WebhookDeliveryDto>>,
    IRequestHandler<RetryWebhookDeliveryCommand, Result> {
    public const int MaxWebhooksPerCompany = 10;
    public const int RecentDeliveryCount = 25;
    private const string EmployerOnly = "Only an employer can manage webhooks.";
    private const string NotFound = "Webhook was not found.";

    private bool IsEmployer => currentUser.Role == UserRole.Employer;

    public async Task<Result<WebhooksOverviewDto>> Handle(GetWebhooksQuery request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result<WebhooksOverviewDto>.Failure(EmployerOnly, ResultErrorType.Forbidden);

        var webhooks = await unitOfWork.Webhooks.GetAllAsync(cancellationToken);
        var names = webhooks.ToDictionary(w => w.Id, w => w.Name);
        var deliveries = await unitOfWork.Webhooks.GetRecentDeliveriesAsync(RecentDeliveryCount, cancellationToken);
        return Result<WebhooksOverviewDto>.Success(new WebhooksOverviewDto(
            WebhookEvents.All,
            webhooks.Select(WebhookDto.From).ToList(),
            deliveries.Select(d => WebhookDeliveryDto.From(d, names.GetValueOrDefault(d.WebhookId, ""))).ToList()));
    }

    public async Task<Result<WebhookDto>> Handle(SaveWebhookCommand request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result<WebhookDto>.Failure(EmployerOnly, ResultErrorType.Forbidden);

        Webhook webhook;
        if (request.Id is { } id) {
            var existing = await unitOfWork.Webhooks.GetByIdAsync(id, cancellationToken);
            if (existing is null) return Result<WebhookDto>.Failure(NotFound, ResultErrorType.NotFound);
            webhook = existing;
            webhook.Update(request.Name, request.Url, request.Events, request.IsEnabled);
        } else {
            if ((await unitOfWork.Webhooks.GetAllAsync(cancellationToken)).Count >= MaxWebhooksPerCompany) {
                return Result<WebhookDto>.Failure($"A company can have at most {MaxWebhooksPerCompany} webhooks.");
            }

            webhook = Webhook.Create(request.Name, request.Url, request.Events, secretProtector.Protect(NewSecret()));
            webhook.Update(request.Name, request.Url, request.Events, request.IsEnabled);
            await unitOfWork.Webhooks.AddAsync(webhook, cancellationToken);
        }

        var headerName = string.IsNullOrWhiteSpace(request.HeaderName) ? null : request.HeaderName.Trim();
        // A value isn't trimmed: spaces can be part of a real key. Renaming the header needs its value again.
        var renamed = headerName is not null && !string.Equals(headerName, webhook.HeaderName, StringComparison.OrdinalIgnoreCase);
        if (headerName is not null && string.IsNullOrEmpty(request.HeaderValue) && (renamed || webhook.HeaderValueProtected is null)) {
            return Result<WebhookDto>.Failure("Enter a value for the extra header.");
        }

        webhook.SetHeader(headerName, string.IsNullOrEmpty(request.HeaderValue) ? null : secretProtector.Protect(request.HeaderValue));

        await audit.RecordAsync(request.Id is null ? AuditAction.WebhookCreated : AuditAction.WebhookUpdated, null,
            Details(webhook), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<WebhookDto>.Success(WebhookDto.From(webhook));
    }

    public async Task<Result> Handle(DeleteWebhookCommand request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result.Failure(EmployerOnly, ResultErrorType.Forbidden);

        var webhook = await unitOfWork.Webhooks.GetByIdAsync(request.Id, cancellationToken);
        if (webhook is null) return Result.Failure(NotFound, ResultErrorType.NotFound);

        await unitOfWork.Webhooks.RemoveAsync(webhook, cancellationToken);
        await audit.RecordAsync(AuditAction.WebhookDeleted, null, Details(webhook), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<WebhookSecretDto>> Handle(GetWebhookSecretQuery request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result<WebhookSecretDto>.Failure(EmployerOnly, ResultErrorType.Forbidden);

        var webhook = await unitOfWork.Webhooks.GetByIdAsync(request.Id, cancellationToken);
        if (webhook is null) return Result<WebhookSecretDto>.Failure(NotFound, ResultErrorType.NotFound);

        try {
            return Result<WebhookSecretDto>.Success(new WebhookSecretDto(secretProtector.Unprotect(webhook.SecretProtected)));
        } catch (CryptographicException) {
            return Result<WebhookSecretDto>.Failure(
                "The secret can no longer be decrypted (the server's encryption keys changed). Create a new one.");
        }
    }

    public async Task<Result<WebhookSecretDto>> Handle(RotateWebhookSecretCommand request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result<WebhookSecretDto>.Failure(EmployerOnly, ResultErrorType.Forbidden);

        var webhook = await unitOfWork.Webhooks.GetByIdAsync(request.Id, cancellationToken);
        if (webhook is null) return Result<WebhookSecretDto>.Failure(NotFound, ResultErrorType.NotFound);

        var secret = NewSecret();
        webhook.SetSecret(secretProtector.Protect(secret));
        await audit.RecordAsync(AuditAction.WebhookUpdated, null, Details(webhook), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<WebhookSecretDto>.Success(new WebhookSecretDto(secret));
    }

    public async Task<Result<WebhookDeliveryDto>> Handle(TestWebhookCommand request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result<WebhookDeliveryDto>.Failure(EmployerOnly, ResultErrorType.Forbidden);

        var webhook = await unitOfWork.Webhooks.GetByIdAsync(request.Id, cancellationToken);
        if (webhook is null) return Result<WebhookDeliveryDto>.Failure(NotFound, ResultErrorType.NotFound);

        var now = clock.UtcNow;
        var eventId = Guid.NewGuid();
        var delivery = WebhookDelivery.Create(webhook.Id, eventId, WebhookEvents.Test, "Test",
            WebhookPayloads.Serialize(WebhookPayloads.TestEvent(eventId, now, currentUser.FullName)), now);
        // Sent right away, and not retried: the employer is looking at the result.
        await processor.SendAndRecordAsync(webhook, delivery, cancellationToken, retry: false);
        await unitOfWork.Webhooks.AddDeliveryAsync(delivery, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<WebhookDeliveryDto>.Success(WebhookDeliveryDto.From(delivery, webhook.Name));
    }

    public async Task<Result> Handle(RetryWebhookDeliveryCommand request, CancellationToken cancellationToken) {
        if (!IsEmployer) return Result.Failure(EmployerOnly, ResultErrorType.Forbidden);

        var delivery = await unitOfWork.Webhooks.GetDeliveryAsync(request.Id, cancellationToken);
        if (delivery is null) return Result.Failure("That call was not found.", ResultErrorType.NotFound);
        if (delivery.EventType == WebhookEvents.Test) return Result.Failure("Use Send test to try the webhook again.");
        if (delivery.Status != WebhookDeliveryStatus.Failed) return Result.Failure("Only failed calls can be sent again.", ResultErrorType.Conflict);

        delivery.Retry(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>A random secret like "whsec_…"; shown to employers so the receiver can check signatures.</summary>
    private static string NewSecret() =>
        "whsec_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static Dictionary<string, string> Details(Webhook webhook) =>
        new() { ["name"] = webhook.Name, ["url"] = webhook.Url };
}
