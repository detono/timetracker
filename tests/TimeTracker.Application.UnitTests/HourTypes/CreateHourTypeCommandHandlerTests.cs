using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Commands.CreateHourType;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.HourTypes;

public class CreateHourTypeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHourTypeRepository> _hourTypeRepository = new();

    public CreateHourTypeCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.HourTypes).Returns(_hourTypeRepository.Object);
    }

    [Fact]
    public async Task Handle_AsEmployer_Succeeds()
    {
        _hourTypeRepository.Setup(r => r.NameExistsAsync("PTO", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object, new TestCurrentUserService(Guid.NewGuid(), UserRole.Employer));
        var result = await handler.Handle(new CreateHourTypeCommand("PTO", "#2f6f62"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("PTO");
        _hourTypeRepository.Verify(r => r.AddAsync(It.IsAny<HourType>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AsEmployee_ReturnsForbidden()
    {
        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object, new TestCurrentUserService(Guid.NewGuid(), UserRole.Employee));
        var result = await handler.Handle(new CreateHourTypeCommand("PTO", "#2f6f62"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        _hourTypeRepository.Setup(r => r.NameExistsAsync("PTO", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object, new TestCurrentUserService(Guid.NewGuid(), UserRole.Employer));
        var result = await handler.Handle(new CreateHourTypeCommand("PTO", "#2f6f62"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }
}
