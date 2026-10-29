using FluentAssertions;
using TimeTracker.Application.Auth;
using Xunit;

namespace TimeTracker.Application.UnitTests.Auth;

public class PasswordResetCodesTests
{
    [Fact]
    public void Generate_MakesEightCharacterCodesWithoutLookAlikes()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => PasswordResetCodes.Generate()).ToList();

        codes.Should().AllSatisfy(c => c.Should().MatchRegex("^[A-HJKMNP-Z2-9]{4}-[A-HJKMNP-Z2-9]{4}$"));
        codes.Distinct().Should().HaveCount(codes.Count);
    }

    [Theory]
    [InlineData("K7QM-3XTP")]
    [InlineData("k7qm3xtp")]
    [InlineData(" k7qm 3xtp ")]
    public void Normalize_ForgivesCaseSpacesAndDashes(string typed)
    {
        PasswordResetCodes.Normalize(typed).Should().Be("K7QM3XTP");
    }
}
