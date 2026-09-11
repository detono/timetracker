using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Commands.AssignSupervisor;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.Users;

public class AssignSupervisorCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    public AssignSupervisorCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_EmployerAssigningEmployeeSupervisor_Succeeds()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var lead = User.Create("Cara", "Lead", "cara@co.com", "hash", UserRole.Employee);

        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _userRepository.Setup(r => r.GetByIdAsync(lead.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lead);

        var handler = new AssignSupervisorCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new AssignSupervisorCommand(employee.Id, lead.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        employee.SupervisorId.Should().Be(lead.Id);
    }

    [Fact]
    public async Task Handle_SupervisorIsEmployer_ReturnsFailure()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var otherEmployer = User.Create("Zoe", "Boss2", "zoe@co.com", "hash", UserRole.Employer);

        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _userRepository.Setup(r => r.GetByIdAsync(otherEmployer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherEmployer);

        var handler = new AssignSupervisorCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new AssignSupervisorCommand(employee.Id, otherEmployer.Id), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TargetIsEmployer_ReturnsFailure()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var otherEmployer = User.Create("Zoe", "Boss2", "zoe@co.com", "hash", UserRole.Employer);

        _userRepository.Setup(r => r.GetByIdAsync(otherEmployer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherEmployer);

        var handler = new AssignSupervisorCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new AssignSupervisorCommand(otherEmployer.Id, null), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonEmployerCaller_ReturnsForbidden()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);

        var handler = new AssignSupervisorCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employee.Id, UserRole.Employee));
        var result = await handler.Handle(new AssignSupervisorCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }
}
