using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PlatformAdmins.Commands.SetPlatformAdminActive;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.PlatformAdmins;

public class SetPlatformAdminActiveCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    public SetPlatformAdminActiveCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_DeactivatingSelf_ReturnsValidationFailure()
    {
        var self = User.CreatePlatformAdmin("Pat", "Admin", "pat@platform.com", "hash");
        var handler = new SetPlatformAdminActiveCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(self.Id, UserRole.PlatformAdmin));

        var result = await handler.Handle(new SetPlatformAdminActiveCommand(self.Id, false), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public async Task Handle_DeactivatingOtherAdmin_Succeeds()
    {
        var other = User.CreatePlatformAdmin("Sam", "Admin", "sam@platform.com", "hash");
        _userRepository.Setup(r => r.GetByIdAsync(other.Id, It.IsAny<CancellationToken>())).ReturnsAsync(other);
        var handler = new SetPlatformAdminActiveCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.PlatformAdmin));

        var result = await handler.Handle(new SetPlatformAdminActiveCommand(other.Id, false), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        other.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TargetIsNotPlatformAdmin_ReturnsNotFound()
    {
        var employee = User.Create("Jane", "Doe", "jane@acme.com", "hash", UserRole.Employee, Guid.NewGuid());
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        var handler = new SetPlatformAdminActiveCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.PlatformAdmin));

        var result = await handler.Handle(new SetPlatformAdminActiveCommand(employee.Id, false), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        employee.IsActive.Should().BeTrue();
    }
}
