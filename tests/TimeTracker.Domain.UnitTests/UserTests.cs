using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_CreatesUser()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);

        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("jane@doe.com");
        user.Role.Should().Be(UserRole.Employee);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_Throws()
    {
        var act = () => User.Create("Jane", "Doe", "jane@doe.com", "", UserRole.Employee);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void SetEmail_WithInvalidEmail_Throws(string email)
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);

        var act = () => user.SetEmail(email);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignSupervisor_ToSelf_Throws()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);

        var act = () => user.AssignSupervisor(user.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CanViewHoursOf_Employer_CanViewAnyone()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);

        employer.CanViewHoursOf(Guid.NewGuid(), null).Should().BeTrue();
    }

    [Fact]
    public void CanViewHoursOf_Self_ReturnsTrue()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);

        employee.CanViewHoursOf(employee.Id, null).Should().BeTrue();
    }

    [Fact]
    public void CanViewHoursOf_Supervisor_ReturnsTrue()
    {
        var supervisor = User.Create("Lead", "Person", "lead@co.com", "hash", UserRole.Employee);
        var targetId = Guid.NewGuid();

        supervisor.CanViewHoursOf(targetId, supervisor.Id).Should().BeTrue();
    }

    [Fact]
    public void CanViewHoursOf_UnrelatedEmployee_ReturnsFalse()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);

        employee.CanViewHoursOf(Guid.NewGuid(), Guid.NewGuid()).Should().BeFalse();
    }
}
