using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class ProjectTests {
    [Fact]
    public void Create_KeepsBudgetAndBillable() {
        var project = Project.Create("Website", "Acme", budgetHours: 120, isBillable: true);

        project.BudgetHours.Should().Be(120);
        project.IsBillable.Should().BeTrue();
        project.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100_001)]
    public void Budget_MustBePositiveAndNotHuge(decimal budget) {
        var create = () => Project.Create("Website", budgetHours: budget);
        create.Should().Throw<DomainException>();

        var project = Project.Create("Website");
        var update = () => project.Update("Website", null, budget);
        update.Should().Throw<DomainException>();
    }

    [Fact]
    public void WithoutMembers_EveryoneMayLog() {
        var project = Project.Create("Website");

        project.IsRestricted.Should().BeFalse();
        project.IsOpenTo(Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public void WithMembers_OnlyTheyMayLog() {
        var member = Guid.NewGuid();
        var project = Project.Create("Website");

        project.SetMembers(new[] { member, member });

        project.IsRestricted.Should().BeTrue();
        project.Members.Should().ContainSingle().Which.UserId.Should().Be(member);
        project.IsOpenTo(member).Should().BeTrue();
        project.IsOpenTo(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void SetMembers_KeepsThoseWhoStay() {
        var stays = Guid.NewGuid();
        var leaves = Guid.NewGuid();
        var joins = Guid.NewGuid();
        var project = Project.Create("Website");
        project.SetMembers(new[] { stays, leaves });
        var kept = project.Members.Single(m => m.UserId == stays);

        project.SetMembers(new[] { stays, joins });

        project.Members.Select(m => m.UserId).Should().BeEquivalentTo(new[] { stays, joins });
        project.Members.Should().Contain(m => ReferenceEquals(m, kept));
    }

    [Fact]
    public void SetMembers_WithNobody_OpensItToEveryoneAgain() {
        var project = Project.Create("Website");
        project.SetMembers(new[] { Guid.NewGuid() });

        project.SetMembers(Array.Empty<Guid>());

        project.IsRestricted.Should().BeFalse();
    }

    [Fact]
    public void SetMembers_RejectsAnEmptyId() {
        var project = Project.Create("Website");
        var act = () => project.SetMembers(new[] { Guid.Empty });
        act.Should().Throw<DomainException>();
    }
}
