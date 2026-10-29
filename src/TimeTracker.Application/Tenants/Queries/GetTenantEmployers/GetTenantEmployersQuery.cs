using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Tenants.Queries.GetTenantEmployers;

/// <summary>Platform admin only: a company's employer accounts, e.g. to reset a password.</summary>
public record GetTenantEmployersQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<TenantEmployerDto>>>;

public class GetTenantEmployersQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ITenantScope tenantScope
) : IRequestHandler<GetTenantEmployersQuery, Result<IReadOnlyList<TenantEmployerDto>>> {
    public async Task<Result<IReadOnlyList<TenantEmployerDto>>> Handle(
        GetTenantEmployersQuery request, CancellationToken cancellationToken
    ) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result<IReadOnlyList<TenantEmployerDto>>.Failure(
                "Only a platform admin can see a company's employers.", ResultErrorType.Forbidden);
        }

        if (await unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken) is null) {
            return Result<IReadOnlyList<TenantEmployerDto>>.Failure("Company was not found.", ResultErrorType.NotFound);
        }

        using var scope = tenantScope.Enter(request.TenantId);
        var employers = (await unitOfWork.Users.GetAllAsync(cancellationToken))
            .Where(u => u.Role == UserRole.Employer)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new TenantEmployerDto(u.Id, $"{u.FirstName} {u.LastName}", u.Email, u.IsActive))
            .ToList();

        return Result<IReadOnlyList<TenantEmployerDto>>.Success(employers);
    }
}
