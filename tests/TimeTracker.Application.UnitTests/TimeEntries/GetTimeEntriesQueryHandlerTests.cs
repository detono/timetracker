using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.TimeEntries.Queries.GetTimeEntries;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.TimeEntries;

public class GetTimeEntriesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITimeEntryRepository> _timeEntryRepository = new();

    public GetTimeEntriesQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.TimeEntries).Returns(_timeEntryRepository.Object);
    }

    [Fact]
    public async Task Handle_SelfRequest_ReturnsEntries()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _timeEntryRepository
            .Setup(r => r.GetForUserAsync(employee.Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>
            {
                TimeEntry.Create(employee.Id, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0))
            });

        var handler = new GetTimeEntriesQueryHandler(_unitOfWork.Object, new TestCurrentUserService(employee.Id, UserRole.Employee));
        var result = await handler.Handle(new GetTimeEntriesQuery(employee.Id, null, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_UnrelatedEmployeeRequestingAnothersHours_ReturnsForbidden()
    {
        var requester = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var target = User.Create("Charlie", "Other", "charlie@co.com", "hash", UserRole.Employee);

        _userRepository.Setup(r => r.GetByIdAsync(requester.Id, It.IsAny<CancellationToken>())).ReturnsAsync(requester);
        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var handler = new GetTimeEntriesQueryHandler(_unitOfWork.Object, new TestCurrentUserService(requester.Id, UserRole.Employee));
        var result = await handler.Handle(new GetTimeEntriesQuery(target.Id, null, null), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_SupervisorRequestingSuperviseesHours_Succeeds()
    {
        var supervisor = User.Create("Lead", "Person", "lead@co.com", "hash", UserRole.Employee);
        var employee = User.Create("Charlie", "Worker", "charlie@co.com", "hash", UserRole.Employee);
        employee.AssignSupervisor(supervisor.Id);

        _userRepository.Setup(r => r.GetByIdAsync(supervisor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(supervisor);
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _timeEntryRepository
            .Setup(r => r.GetForUserAsync(employee.Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>());

        var handler = new GetTimeEntriesQueryHandler(_unitOfWork.Object, new TestCurrentUserService(supervisor.Id, UserRole.Employee));
        var result = await handler.Handle(new GetTimeEntriesQuery(employee.Id, null, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Employer_CanViewAnyEmployee()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Charlie", "Worker", "charlie@co.com", "hash", UserRole.Employee);

        _userRepository.Setup(r => r.GetByIdAsync(employer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employer);
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _timeEntryRepository
            .Setup(r => r.GetForUserAsync(employee.Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>());

        var handler = new GetTimeEntriesQueryHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new GetTimeEntriesQuery(employee.Id, null, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
