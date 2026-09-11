using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Commands.DeactivateUser;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.Users;

public class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    public DeactivateUserCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_NonEmployer_ReturnsForbidden()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var target = User.Create("Charlie", "Other", "charlie@co.com", "hash", UserRole.Employee);

        var handler = new DeactivateUserCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employee.Id, UserRole.Employee));
        var result = await handler.Handle(new DeactivateUserCommand(target.Id), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_EmployerDeactivatingSelf_ReturnsFailure()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);

        var handler = new DeactivateUserCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new DeactivateUserCommand(employer.Id), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmployerDeactivatingEmployee_Succeeds()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var handler = new DeactivateUserCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new DeactivateUserCommand(employee.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        employee.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
