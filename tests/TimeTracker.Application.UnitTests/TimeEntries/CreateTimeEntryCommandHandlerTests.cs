using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.TimeEntries;

public class CreateTimeEntryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITimeEntryRepository> _timeEntryRepository = new();
    private readonly Mock<IHourTypeRepository> _hourTypeRepository = new();
    private readonly HourType _workType = HourType.Create("Work", "#932e4a");

    public CreateTimeEntryCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.TimeEntries).Returns(_timeEntryRepository.Object);
        _unitOfWork.SetupGet(u => u.HourTypes).Returns(_hourTypeRepository.Object);
        _hourTypeRepository.Setup(r => r.GetByIdAsync(_workType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_workType);
    }

    [Fact]
    public async Task Handle_LoggingForSelf_Succeeds()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object, new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, _workType.Id, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0), 30, "Notes");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.DurationHours.Should().Be(7.5);
        result.Value.HourTypeName.Should().Be("Work");
        _timeEntryRepository.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeLoggingForSomeoneElse_ReturnsForbidden()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var otherUserId = Guid.NewGuid();

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employee.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(otherUserId, _workType.Id, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        _timeEntryRepository.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmployerLoggingForSomeoneElse_Succeeds()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var command = new CreateTimeEntryCommand(employee.Id, _workType.Id, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TargetUserNotFound_ReturnsNotFound()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var missingUserId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(missingUserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var command = new CreateTimeEntryCommand(missingUserId, _workType.Id, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_UnknownHourType_ReturnsValidationFailure()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var unknownTypeId = Guid.NewGuid();

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object, new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, unknownTypeId, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _timeEntryRepository.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivatedHourType_ReturnsValidationFailure()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var retiredType = HourType.Create("Retired Type", "#000000");
        retiredType.Deactivate();
        _hourTypeRepository.Setup(r => r.GetByIdAsync(retiredType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(retiredType);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object, new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, retiredType.Id, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }
}
