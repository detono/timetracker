using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Commands.CreateHourType;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.HourTypes;

public class CreateHourTypeCommandHandlerTests {
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IHourTypeRepository> _hourTypeRepository = new();

    // Helper dictionary for our tests
    private readonly Dictionary<string, string> _ptoNames = new() { { "en", "PTO" } };

    public CreateHourTypeCommandHandlerTests() {
        _unitOfWork.SetupGet(u => u.HourTypes).Returns(_hourTypeRepository.Object);
    }

    [Fact]
    public async Task Handle_AsEmployer_Succeeds() {
        _hourTypeRepository.Setup(r => r.NameExistsAsync("PTO", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hourTypeRepository.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HourType>());
        
        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.Employer));
        var result = await handler.Handle(new CreateHourTypeCommand(_ptoNames, "#2f6f62", true), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        // Check the dictionary instead of the old Name property
        result.Value!.LocalizedNames["en"].Should().Be("PTO");
        _hourTypeRepository.Verify(r => r.AddAsync(It.IsAny<HourType>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AsEmployee_ReturnsForbidden() {
        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.Employee));
        var result = await handler.Handle(new CreateHourTypeCommand(_ptoNames, "#2f6f62", true), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict() {
        _hourTypeRepository.Setup(r => r.NameExistsAsync("PTO", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(Guid.NewGuid(), UserRole.Employer));
        var result = await handler.Handle(new CreateHourTypeCommand(_ptoNames, "#2f6f62", true), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }
    
    [Fact]
    public async Task Handle_WhenSettingNewDefault_ClearsOldDefault() {
        // Arrange
        var user = User.Create("Kim", "Stevens", "kim@test.com", "hash", UserRole.Employer);
        var currentUserService = new TestCurrentUserService(user.Id, UserRole.Employer);
    
        // An existing default in the DB
        var oldDefault = HourType.Create(new Dictionary<string, string> { { "en", "Old Default" } }, "#000000", true);
    
        _hourTypeRepository.Setup(x => x.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _hourTypeRepository.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HourType> { oldDefault });

        var handler = new CreateHourTypeCommandHandler(_unitOfWork.Object, currentUserService);
        var command = new CreateHourTypeCommand(new Dictionary<string, string> { { "en", "New Default" } }, "#111111", true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
    
        // Verify the old default was updated to be false
        oldDefault.IsDefault.Should().BeFalse();
        _hourTypeRepository.Verify(x => x.Update(oldDefault), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}