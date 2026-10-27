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
}
