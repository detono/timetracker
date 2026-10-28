using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class TenantTests
{
    [Fact]
    public void Create_TrimsNameAndIsActive()
    {
        var tenant = Tenant.Create("  Acme  ");

        tenant.Name.Should().Be("Acme");
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithBlankName_Throws()
    {
        var act = () => Tenant.Create(" ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignToTenant_ToAnotherTenant_Throws()
    {
        var project = Project.Create("Website");
        project.AssignToTenant(Guid.NewGuid());

        var act = () => project.AssignToTenant(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignToTenant_SameTenantTwice_IsAllowed()
    {
        var tenantId = Guid.NewGuid();
        var project = Project.Create("Website");
        project.AssignToTenant(tenantId);

        project.AssignToTenant(tenantId);

        project.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void SetAddress_NormalizesCountryAndRegion() {
        var tenant = Tenant.Create("Acme");

        tenant.SetAddress(" Kerkstraat 1 ", "9000", "Gent", "be", null);

        tenant.Street.Should().Be("Kerkstraat 1");
        tenant.CountryCode.Should().Be("BE");
        tenant.Region.Should().BeNull();

        tenant.SetAddress(null, null, "München", "DE", "de-by");
        tenant.Region.Should().Be("DE-BY");
    }

    [Theory]
    [InlineData("BEL", null)] // not a two-letter code
    [InlineData("BE", "DE-BY")] // region of another country
    [InlineData(null, "DE-BY")] // region without a country
    public void SetAddress_RejectsInvalidCountryOrRegion(string? country, string? region) {
        var act = () => Tenant.Create("Acme").SetAddress(null, null, null, country, region);

        act.Should().Throw<DomainException>();
    }
}
