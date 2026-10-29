using MediatR;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Queries.GetPasswordResetAvailability;

/// <summary>
/// Whether "forgot password" can work at all on this installation: some active company has
/// email set up. The login page only offers it then.
/// </summary>
public record GetPasswordResetAvailabilityQuery : IRequest<Result<PasswordResetAvailabilityDto>>;

public record PasswordResetAvailabilityDto(bool Available);

public class GetPasswordResetAvailabilityQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPasswordResetAvailabilityQuery, Result<PasswordResetAvailabilityDto>> {
    public async Task<Result<PasswordResetAvailabilityDto>> Handle(
        GetPasswordResetAvailabilityQuery request, CancellationToken cancellationToken
    ) => Result<PasswordResetAvailabilityDto>.Success(
        new PasswordResetAvailabilityDto(await unitOfWork.MailSettings.AnyEnabledAsync(cancellationToken)));
}
