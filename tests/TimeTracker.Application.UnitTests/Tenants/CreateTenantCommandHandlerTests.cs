using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Commands.CreateTenant;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.Tenants;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IHourTypeRepository> _hourTypeRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private readonly CreateTenantCommand _command =
        new("Acme", "Jane", "Owner", "jane@acme.com", "Password123!");

    public CreateTenantCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Tenants).Returns(_tenantRepository.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.HourTypes).Returns(_hourTypeRepository.Object);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash");
    }

    private CreateTenantCommandHandler HandlerFor(UserRole role, Guid? tenantId = null) =>
        new(_unitOfWork.Object, new TestCurrentUserService(Guid.NewGuid(), role, tenantId), _passwordHasher.Object);

    [Fact]
    public async Task Handle_AsPlatformAdmin_CreatesTenantEmployerAndDefaultHourTypes()
    {
        Tenant? savedTenant = null;
        User? savedEmployer = null;
        var savedHourTypes = new List<HourType>();
        _tenantRepository.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((t, _) => savedTenant = t);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => savedEmployer = u);
        _hourTypeRepository.Setup(r => r.AddAsync(It.IsAny<HourType>(), It.IsAny<CancellationToken>()))
            .Callback<HourType, CancellationToken>((h, _) => savedHourTypes.Add(h));

        var result = await HandlerFor(UserRole.PlatformAdmin).Handle(_command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        savedTenant!.Name.Should().Be("Acme");
        savedEmployer!.Role.Should().Be(UserRole.Employer);
        savedEmployer.TenantId.Should().Be(savedTenant.Id);
        savedHourTypes.Should().HaveCount(3).And.OnlyContain(h => h.TenantId == savedTenant.Id);
        savedHourTypes.Should().ContainSingle(h => h.IsDefault);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AsEmployer_ReturnsForbidden()
    {
        var result = await HandlerFor(UserRole.Employer, Guid.NewGuid()).Handle(_command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        _tenantRepository.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsed_ReturnsConflict()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("jane@acme.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await HandlerFor(UserRole.PlatformAdmin).Handle(_command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_NameAlreadyUsed_ReturnsConflict()
    {
        _tenantRepository.Setup(r => r.NameExistsAsync("Acme", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await HandlerFor(UserRole.PlatformAdmin).Handle(_command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }
}
