using FluentAssertions;
using TimeTracker.Application.WhatsNew;
using Xunit;

namespace TimeTracker.Application.UnitTests.WhatsNew;

public class ReleaseNotesTests
{
    private const string Changelog = """
        # Changelog

        Intro text that isn't part of a release.

        ## [Unreleased]

        ### Added

        - Something not released yet.

        ## [1.10.0] - 2026-11-10

        ### Added

        - **Webhooks**: the app calls the addresses you choose,
          so another system can follow along.
        - External IDs.

        ### For developers

        - `PUT /api/users/{id}/external-id` sets it.

        ### Fixed

        - Choosing a supervisor failed.

        ## [1.9] - 2026-11-01

        ### For developers

        - Only for developers, so this release has no sections left.

        ## [0.2.1] - 2026-09-11

        ### Changed

        - Older change.

        [Unreleased]: https://github.com/example/timetracker/compare/v1.10.0...HEAD
        [1.10.0]: https://github.com/example/timetracker/compare/v1.9...v1.10.0
        """;

    [Fact]
    public void ReadsReleasedVersions_NewestFirst_WithoutUnreleased()
    {
        var notes = ReleaseNotes.Parse(Changelog);

        notes.Releases.Select(r => r.Version).Should().Equal("1.10.0", "1.9", "0.2.1");
        notes.Releases[0].Date.Should().Be(new DateOnly(2026, 11, 10));
    }

    [Fact]
    public void JoinsWrappedLines_AndLeavesOutTheDeveloperSection()
    {
        var newest = ReleaseNotes.Parse(Changelog).Releases[0];

        newest.Sections.Select(s => s.Title).Should().Equal("Added", "Fixed");
        newest.Sections[0].Items.Should().Equal(
            "**Webhooks**: the app calls the addresses you choose, so another system can follow along.",
            "External IDs.");
        newest.Sections[1].Items.Should().Equal("Choosing a supervisor failed.");
    }

    [Fact]
    public void KeepsAReleaseWithOnlyDeveloperNotes_WithoutSections()
    {
        ReleaseNotes.Parse(Changelog).Releases[1].Sections.Should().BeEmpty();
    }

    [Fact]
    public void LinkDefinitionsAtTheBottom_AreNotPartOfTheLastRelease()
    {
        ReleaseNotes.Parse(Changelog).Releases[2].Sections.Single().Items.Should().Equal("Older change.");
    }

    [Theory]
    [InlineData("1.4", "1.4.0")]
    [InlineData("v1.4.0", "1.4.0")]
    [InlineData("2", "2.0.0")]
    [InlineData(" 1.10.3 ", "1.10.3")]
    public void ParsesVersions(string text, string expected)
    {
        ReleaseNotes.TryParseVersion(text, out var version).Should().BeTrue();
        version.Should().Be(Version.Parse(expected));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unreleased")]
    public void RejectsWhatIsntAVersion(string? text)
    {
        ReleaseNotes.TryParseVersion(text, out _).Should().BeFalse();
    }

    [Fact]
    public void TheBuiltInChangelog_HasReleases()
    {
        var notes = ReleaseNotes.LoadEmbedded();

        notes.Releases.Should().NotBeEmpty();
        notes.Releases.Should().Contain(r => r.Version == "1.0.0");
        notes.Releases.Should().BeInDescendingOrder(r => r.Number);
    }
}
