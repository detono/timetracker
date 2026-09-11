using FluentAssertions;
using Moq;
using TimeTracker.Application.Auth.Commands.Login;
using TimeTracker.Application.Common.Interfaces;
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

    public LoginCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
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
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        _userRepository.Setup(r => r.GetByEmailAsync("jane@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("wrong", "hash")).Returns(false);

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("jane@doe.com", "wrong"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsFailure()
    {
        var user = User.Create("Jane", "Doe", "jane@doe.com", "hash", UserRole.Employee);
        user.Deactivate();
        _userRepository.Setup(r => r.GetByEmailAsync("jane@doe.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new LoginCommandHandler(_unitOfWork.Object, _passwordHasher.Object, _tokenService.Object);
        var result = await handler.Handle(new LoginCommand("jane@doe.com", "password"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }
}
