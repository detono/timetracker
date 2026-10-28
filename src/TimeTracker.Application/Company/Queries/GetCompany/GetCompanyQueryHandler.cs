using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Company.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Company.Queries.GetCompany;

public class GetCompanyQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetCompanyQuery, Result<CompanyDto>> {
    public async Task<Result<CompanyDto>> Handle(GetCompanyQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer || currentUser.TenantId is not { } tenantId) {
            return Result<CompanyDto>.Failure("Only an employer can view the company details.", ResultErrorType.Forbidden);
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        return tenant is null
            ? Result<CompanyDto>.Failure("Company was not found.", ResultErrorType.NotFound)
            : Result<CompanyDto>.Success(tenant.ToCompanyDto());
    }
}
