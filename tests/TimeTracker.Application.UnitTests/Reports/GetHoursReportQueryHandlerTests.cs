using FluentAssertions;
using Moq;
using TimeTracker.Application.Reports.Queries.GetHoursReport;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.Reports;

public class GetHoursReportQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITimeEntryRepository> _timeEntryRepository = new();

    public GetHoursReportQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.TimeEntries).Returns(_timeEntryRepository.Object);
    }

    [Fact]
    public async Task Handle_EmployerGroupedByDay_AggregatesHoursPerEmployeePerDay()
    {
        var employer = User.Create("Alice", "Boss", "alice@co.com", "hash", UserRole.Employer);
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);

        _userRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { employer, employee });

        var from = new DateOnly(2026, 1, 5);
        var to = new DateOnly(2026, 1, 6);

        _timeEntryRepository
            .Setup(r => r.GetForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>
            {
                TimeEntry.Create(employee.Id, from, new TimeOnly(9, 0), new TimeOnly(17, 0)), // 8h
                TimeEntry.Create(employee.Id, from, new TimeOnly(18, 0), new TimeOnly(19, 0)), // 1h same day
                TimeEntry.Create(employee.Id, to, new TimeOnly(9, 0), new TimeOnly(13, 0))     // 4h next day
            });

        var handler = new GetHoursReportQueryHandler(_unitOfWork.Object, new TestCurrentUserService(employer.Id, UserRole.Employer));
        var result = await handler.Handle(new GetHoursReportQuery(from, to, ReportGrouping.Day), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(2);
        result.Value.Lines.First(l => l.PeriodStart == from).TotalHours.Should().Be(9);
        result.Value.Lines.First(l => l.PeriodStart == to).TotalHours.Should().Be(4);
        result.Value.GrandTotalHours.Should().Be(13);
    }

    [Fact]
    public async Task Handle_EmployeeWithoutAuthorityRequestingOthersReport_ReturnsForbidden()
    {
        var employee = User.Create("Bob", "Worker", "bob@co.com", "hash", UserRole.Employee);
        var otherId = Guid.NewGuid();

        _userRepository.Setup(r => r.GetSupervisedByAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _userRepository.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var handler = new GetHoursReportQueryHandler(_unitOfWork.Object, new TestCurrentUserService(employee.Id, UserRole.Employee));
        var result = await handler.Handle(
            new GetHoursReportQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), ReportGrouping.Week, otherId),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }
}
