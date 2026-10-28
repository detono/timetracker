using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace TimeTracker.Application.Common.Localization;

/// <summary>
/// Email texts in one language, read from <c>Resources/Emails/{language}.json</c>. A language
/// without a file, or a key missing from it, falls back to English, so a partial
/// translation still works. Placeholders look like <c>{name}</c>.
/// </summary>
public sealed class EmailTexts {
    public const string DefaultLanguage = "en";

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> All = new(Load);

    private readonly IReadOnlyDictionary<string, string> _texts;
    private readonly IReadOnlyDictionary<string, string> _fallback;

    private EmailTexts(string language, IReadOnlyDictionary<string, string> texts) {
        Language = language;
        _texts = texts;
        _fallback = All.Value[DefaultLanguage];
        Culture = CultureInfo.GetCultureInfo(language);
    }

    /// <summary>The language these texts are in: the requested one, or English if there's no file for it.</summary>
    public string Language { get; }

    /// <summary>For dates and numbers in the email.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Every language that has an email file.</summary>
    public static IReadOnlyCollection<string> AvailableLanguages => All.Value.Keys.ToList();

    /// <summary>Texts for <paramref name="language"/> (e.g. "de"), or English when there's no file for it.</summary>
    public static EmailTexts For(string? language) {
        var code = language?.Trim().ToLowerInvariant();
        return code is not null && All.Value.TryGetValue(code, out var texts)
            ? new EmailTexts(code, texts)
            : new EmailTexts(DefaultLanguage, All.Value[DefaultLanguage]);
    }

    /// <summary>The text for <paramref name="key"/> with its placeholders filled in.</summary>
    public string Get(string key, params (string Name, string Value)[] values) {
        if (!_texts.TryGetValue(key, out var text) && !_fallback.TryGetValue(key, out text)) {
            throw new KeyNotFoundException($"Email text '{key}' is missing from {DefaultLanguage}.json.");
        }

        foreach (var (name, value) in values) {
            text = text.Replace("{" + name + "}", value);
        }

        return text;
    }

    /// <summary>The raw texts per language; for tests that compare the files.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Files => All.Value;

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Load() {
        var assembly = typeof(EmailTexts).Assembly;
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var resource in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith("Emails.") && n.EndsWith(".json"))) {
            var language = resource["Emails.".Length..^".json".Length];
            using var stream = assembly.GetManifestResourceStream(resource)!;
            result[language] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                               ?? new Dictionary<string, string>();
        }

        if (!result.ContainsKey(DefaultLanguage)) {
            throw new InvalidOperationException($"Resources/Emails/{DefaultLanguage}.json is missing.");
        }

        return result;
    }
}
