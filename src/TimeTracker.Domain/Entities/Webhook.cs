using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// An address another system gave, that this app calls whenever one of the chosen events
/// happens (e.g. "leave.approved"). Every call is signed with the webhook's secret, so the
/// receiver can check it came from here. The secret and the optional extra header value are
/// stored already encrypted.
/// </summary>
public class Webhook : TenantOwnedEntity {
    public const int MaxNameLength = 100;
    public const int MaxUrlLength = 2048;
    public const int MaxHeaderNameLength = 100;

    public string Name { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public List<string> Events { get; private set; } = new();
    public bool IsEnabled { get; private set; } = true;
    public string SecretProtected { get; private set; } = default!;

    /// <summary>An extra header sent with every call, e.g. "Authorization", for APIs that need a key.</summary>
    public string? HeaderName { get; private set; }

    public string? HeaderValueProtected { get; private set; }

    private Webhook() {
        // EF Core
    }

    public static Webhook Create(string name, string url, IEnumerable<string> events, string secretProtected) {
        var webhook = new Webhook();
        webhook.Update(name, url, events, true);
        webhook.SetSecret(secretProtected);
        return webhook;
    }

    public void Update(string name, string url, IEnumerable<string> events, bool isEnabled) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A name is required.");
        }

        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) {
            throw new DomainException("The URL must start with https:// or http://.");
        }

        var chosen = events.Distinct().ToList();
        if (chosen.Count == 0) {
            throw new DomainException("Choose at least one event.");
        }

        Name = name.Trim();
        Url = uri.ToString();
        Events = chosen;
        IsEnabled = isEnabled;
        MarkUpdated();
    }

    public void SetSecret(string secretProtected) {
        SecretProtected = secretProtected;
        MarkUpdated();
    }

    /// <summary>
    /// A null <paramref name="name"/> removes the header. A null <paramref name="valueProtected"/>
    /// keeps the stored value, so it doesn't have to be typed again on every save.
    /// </summary>
    public void SetHeader(string? name, string? valueProtected) {
        if (string.IsNullOrWhiteSpace(name)) {
            HeaderName = null;
            HeaderValueProtected = null;
        } else {
            if (valueProtected is null && HeaderValueProtected is null) {
                throw new DomainException("Enter a value for the extra header.");
            }

            HeaderName = name.Trim();
            if (valueProtected is not null) {
                HeaderValueProtected = valueProtected;
            }
        }

        MarkUpdated();
    }

    public bool Wants(string eventType) => IsEnabled && Events.Contains(eventType);
}
