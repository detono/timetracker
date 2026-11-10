using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TimeTracker.Application.WhatsNew;

/// <summary>One released version from CHANGELOG.md, newest first in <see cref="ReleaseNotes.Releases"/>.</summary>
public record Release(string Version, Version Number, DateOnly Date, IReadOnlyList<ReleaseSection> Sections);

/// <summary>An "Added", "Changed" or "Fixed" group of one release. Items keep their Markdown.</summary>
public record ReleaseSection(string Title, IReadOnlyList<string> Items);

/// <summary>
/// The released versions in CHANGELOG.md, which is built into the app, for the "What's new"
/// notes people see after an update. Reads the Keep a Changelog layout: <c>## [1.4.0] - 2026-11-10</c>,
/// then <c>### Added</c> sections with <c>- </c> items whose next lines are indented.
/// <c>## [Unreleased]</c> and sections called <see cref="DeveloperSection"/> are left out.
/// </summary>
public partial class ReleaseNotes
{
    /// <summary>A section for people who run or build on the app, not for everyday users.</summary>
    public const string DeveloperSection = "For developers";

    private const string ResourceName = "CHANGELOG.md";

    public IReadOnlyList<Release> Releases { get; }

    public ReleaseNotes(IReadOnlyList<Release> releases)
    {
        Releases = releases;
    }

    /// <summary>The changelog built into this assembly; empty if it's missing.</summary>
    public static ReleaseNotes LoadEmbedded()
    {
        using var stream = typeof(ReleaseNotes).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return new ReleaseNotes([]);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Parse(reader.ReadToEnd());
    }

    public static ReleaseNotes Parse(string markdown)
    {
        var releases = new List<Release>();
        ReleaseBuilder? release = null;
        List<string>? items = null;
        StringBuilder? item = null;

        void EndItem()
        {
            if (item is not null && items is not null)
            {
                items.Add(item.ToString());
            }

            item = null;
        }

        void EndRelease()
        {
            EndItem();
            if (release is not null)
            {
                releases.Add(release.Build());
            }

            release = null;
            items = null;
        }

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                EndRelease();
                var heading = ReleaseHeading().Match(line);
                if (heading.Success
                    && TryParseVersion(heading.Groups["version"].Value, out var number)
                    && DateOnly.TryParseExact(heading.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date))
                {
                    release = new ReleaseBuilder(heading.Groups["version"].Value, number, date);
                }

                continue;
            }

            // Link definitions at the bottom ("[1.0.0]: https://…") end the last release.
            if (LinkDefinition().IsMatch(line))
            {
                EndRelease();
                continue;
            }

            if (release is null)
            {
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                EndItem();
                var title = line[4..].Trim();
                items = string.Equals(title, DeveloperSection, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : release.Section(title);
                continue;
            }

            if (items is null)
            {
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                EndItem();
                item = new StringBuilder(line[2..].Trim());
            }
            else if (item is not null && line.StartsWith("  ", StringComparison.Ordinal) && line.Trim().Length > 0)
            {
                item.Append(' ').Append(line.Trim());
            }
            else if (line.Length == 0)
            {
                EndItem();
            }
        }

        EndRelease();
        return new ReleaseNotes(releases.OrderByDescending(r => r.Number).ToList());
    }

    /// <summary>"1.4", "1.4.0" and "v1.4.0" are the same version.</summary>
    public static bool TryParseVersion(string? text, out Version version)
    {
        version = new Version(0, 0, 0);
        var trimmed = text?.Trim().TrimStart('v', 'V');
        if (string.IsNullOrEmpty(trimmed) || !System.Version.TryParse(trimmed.Contains('.') ? trimmed : trimmed + ".0", out var parsed))
        {
            return false;
        }

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        return true;
    }

    [GeneratedRegex(@"^## \[(?<version>[^\]]+)\] - (?<date>\d{4}-\d{2}-\d{2})")]
    private static partial Regex ReleaseHeading();

    [GeneratedRegex(@"^\[[^\]]+\]: \S+")]
    private static partial Regex LinkDefinition();

    private sealed class ReleaseBuilder(string version, Version number, DateOnly date)
    {
        private readonly List<(string Title, List<string> Items)> _sections = [];

        public List<string> Section(string title)
        {
            var items = new List<string>();
            _sections.Add((title, items));
            return items;
        }

        public Release Build() => new(
            version,
            number,
            date,
            _sections.Where(s => s.Items.Count > 0).Select(s => new ReleaseSection(s.Title, s.Items)).ToList());
    }
}
