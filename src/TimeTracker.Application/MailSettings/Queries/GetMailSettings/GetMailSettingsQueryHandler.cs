using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.MailSettings.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.MailSettings.Queries.GetMailSettings;

public class GetMailSettingsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetMailSettingsQuery, Result<MailSettingsDto>> {
    public async Task<Result<MailSettingsDto>> Handle(GetMailSettingsQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<MailSettingsDto>.Failure("Only an employer can view mail settings.", ResultErrorType.Forbidden);
        }

        var settings = await unitOfWork.MailSettings.GetForCurrentTenantAsync(cancellationToken);
        return Result<MailSettingsDto>.Success(settings is null ? MailSettingsDto.NotConfigured() : MailSettingsDto.From(settings));
    }
}
