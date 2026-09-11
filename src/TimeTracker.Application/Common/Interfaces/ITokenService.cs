using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>Issues a signed JWT for the given authenticated user.</summary>
    string GenerateToken(User user);
}
