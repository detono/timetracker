using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetTenantsQuery, Result<IReadOnlyList<TenantDto>>> {
    public async Task<Result<IReadOnlyList<TenantDto>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result<IReadOnlyList<TenantDto>>.Failure("Only a platform admin can list tenants.",
                ResultErrorType.Forbidden);
        }

        var tenants = await unitOfWork.Tenants.GetAllAsync(cancellationToken);
        var userCounts = await unitOfWork.Tenants.GetUserCountsAsync(cancellationToken);

        var dtos = tenants
            .Select(t => new TenantDto(t.Id, t.Name, t.IsActive, userCounts.GetValueOrDefault(t.Id), t.CreatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<TenantDto>>.Success(dtos);
    }
}
