using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Commands.DeactivateHourType;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.HourTypes;

public class DeactivateHourTypeCommandHandlerTests {
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHourTypeRepository> _hourTypeRepository = new();

    public DeactivateHourTypeCommandHandlerTests() {
        _unitOfWork.SetupGet(u => u.HourTypes).Returns(_hourTypeRepository.Object);
    }

    [Fact]
    public async Task Handle_AsEmployer_DeactivatesType() {
        var type = HourType.Create(new Dictionary<string, string> { { "en", "Old Type" } }, "#999999", true);
        _hourTypeRepository.Setup(r => r.GetByIdAsync(type.Id, It.IsAny<CancellationToken>())).ReturnsAsync(type);

        var handler = new DeactivateHourTypeCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.Employer));
        var result = await handler.Handle(new DeactivateHourTypeCommand(type.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        type.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AsEmployee_ReturnsForbidden() {
        var handler = new DeactivateHourTypeCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.Employee));
        var result = await handler.Handle(new DeactivateHourTypeCommand(Guid.NewGuid()), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }
}