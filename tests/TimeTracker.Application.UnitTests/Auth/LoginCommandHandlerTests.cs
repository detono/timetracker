using FluentAssertions;
using Moq;
using TimeTracker.Application.Auth.Commands.Login;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Tenant _tenant = Tenant.Create("Acme");

    public LoginCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.Tenants).Returns(_tenantRepository.Object);
        _tenantRepository.Setup(r => r.GetByIdAsync(_tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_tenant);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee, _tenant.Id);
        _userRepository.Setup(r => r.GetByEmailAsync("jane@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("password", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("fake-jwt");

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("jane@doe.com", "password"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Token.Should().Be("fake-jwt");
        result.Value.Role.Should().Be("Employee");
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsFailureWithoutRevealingReason()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("missing@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("missing@doe.com", "password"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee, _tenant.Id);
        _userRepository.Setup(r => r.GetByEmailAsync("jane@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("wrong", "hash")).Returns(false);

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("jane@doe.com", "wrong"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsFailure()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee, _tenant.Id);
        user.Deactivate();
        _userRepository.Setup(r => r.GetByEmailAsync("jane@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("jane@doe.com", "password"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TenantDeactivated_ReturnsForbidden()
    {
        _tenant.Deactivate();
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee, _tenant.Id);
        _userRepository.Setup(r => r.GetByEmailAsync("jane@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("password", "hash")).Returns(true);

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("jane@doe.com", "password"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        _tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlatformAdmin_ReturnsTokenWithoutTenant()
    {
        var admin = User.CreatePlatformAdmin("Pat", "Admin", "pat@platform.com", "hash");
        _userRepository.Setup(r => r.GetByEmailAsync("pat@platform.com", It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _passwordHasher.Setup(p => p.Verify("password", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(admin)).Returns("admin-jwt");

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("pat@platform.com", "password"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Role.Should().Be("PlatformAdmin");
        result.Value.TenantName.Should().BeNull();
    }
}
