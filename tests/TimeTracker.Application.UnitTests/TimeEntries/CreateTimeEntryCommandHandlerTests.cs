using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.TimeEntries;

public class CreateTimeEntryCommandHandlerTests {
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITimeEntryRepository> _timeEntryRepository = new();
    private readonly Mock<IHourTypeRepository> _hourTypeRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new(); 
    
    // FIX: Provide the dictionary instead of a single string
    private readonly HourType _workType = HourType.Create(
        new Dictionary<string, string> { { "en", "Work" }, { "nl", "Werken" }, { "fr", "Travail" } }, 
        "#932e4a",
        true
    );

    public CreateTimeEntryCommandHandlerTests() {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.TimeEntries).Returns(_timeEntryRepository.Object);
        _unitOfWork.SetupGet(u => u.HourTypes).Returns(_hourTypeRepository.Object);
        _unitOfWork.SetupGet(u => u.Projects).Returns(_projectRepository.Object); 
        _hourTypeRepository.Setup(r => r.GetByIdAsync(_workType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_workType);
    }

    [Fact]
    public async Task Handle_LoggingForSelf_Succeeds() {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, _workType.Id, null, new DateOnly(2026, 1, 5), new TimeOnly(9, 0),
            new TimeOnly(17, 0), 30, "Notes");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.DurationHours.Should().Be(7.5);
        
        result.Value.LocalizedHourTypeNames["en"].Should().Be("Work");
        _timeEntryRepository.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeLoggingForSomeoneElse_ReturnsForbidden() {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var otherUserId = Guid.NewGuid();

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(employee.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(otherUserId, _workType.Id, null, new DateOnly(2026, 1, 5),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        _timeEntryRepository.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmployerLoggingForSomeoneElse_Succeeds() {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(employer.Id, UserRole.Employer));
        var command = new CreateTimeEntryCommand(employee.Id, _workType.Id, null, new DateOnly(2026, 1, 5),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TargetUserNotFound_ReturnsNotFound() {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var missingUserId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(missingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(employer.Id, UserRole.Employer));
        var command = new CreateTimeEntryCommand(missingUserId, _workType.Id, null, new DateOnly(2026, 1, 5),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_UnknownHourType_ReturnsValidationFailure() {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var unknownTypeId = Guid.NewGuid();

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, unknownTypeId, null, new DateOnly(2026, 1, 5),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _timeEntryRepository.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivatedHourType_ReturnsValidationFailure() {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        
        // FIX: Provide the dictionary instead of a single string
        var retiredType = HourType.Create(new Dictionary<string, string> { { "en", "Retired Type" } }, "#000000", true);
        retiredType.Deactivate();
        
        _hourTypeRepository.Setup(r => r.GetByIdAsync(retiredType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retiredType);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, retiredType.Id, null, new DateOnly(2026, 1, 5),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithValidProject_ReturnsDtoWithProjectDetails() {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var project = Project.Create("Internal Migration", "Tradecom NV");
        _projectRepository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, _workType.Id, project.Id, new DateOnly(2026, 9, 13),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 30, "Migration work");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.ProjectId.Should().Be(project.Id);
        result.Value.ProjectName.Should().Be("Internal Migration");
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ReturnsDtoWithNullProjectName() {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var missingProjectId = Guid.NewGuid();
        _projectRepository.Setup(r => r.GetByIdAsync(missingProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, _workType.Id, missingProjectId, new DateOnly(2026, 9, 13),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 30, "Lost project");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.ProjectId.Should().Be(missingProjectId);
        result.Value.ProjectName.Should().BeNull();
    }
    
    [Fact]
    public async Task Handle_HourTypeMissingEnglishTranslation_ReturnsUnknownForName() {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        
        // Create an HourType that deliberately lacks an "en" key
        var frenchOnlyType = HourType.Create(new Dictionary<string, string> { { "fr", "Réunion" } }, "#123456", true);
        
        _hourTypeRepository.Setup(r => r.GetByIdAsync(frenchOnlyType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(frenchOnlyType);

        var handler = new CreateTimeEntryCommandHandler(_unitOfWork.Object,
            new TestCurrentUserService(user.Id, UserRole.Employee));
        var command = new CreateTimeEntryCommand(null, frenchOnlyType.Id, null, new DateOnly(2026, 9, 13),
            new TimeOnly(9, 0), new TimeOnly(17, 0), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.LocalizedHourTypeNames.Should().ContainKey("fr").WhoseValue.Should().Be("Réunion");
        result.Value!.LocalizedHourTypeNames.Should().NotContainKey("en");
    }
}