using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Company.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Company.Commands.UpdateCompany;

public class UpdateCompanyCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateCompanyCommand, Result<CompanyDto>> {
    public async Task<Result<CompanyDto>> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer || currentUser.TenantId is not { } tenantId) {
            return Result<CompanyDto>.Failure("Only an employer can change the company details.", ResultErrorType.Forbidden);
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) {
            return Result<CompanyDto>.Failure("Company was not found.", ResultErrorType.NotFound);
        }

        if (await unitOfWork.Tenants.NameUsedByOtherAsync(request.Name, tenant.Id, cancellationToken)) {
            return Result<CompanyDto>.Failure("Another company already uses this name.", ResultErrorType.Conflict);
        }

        tenant.Rename(request.Name);
        tenant.SetAddress(request.Street, request.PostalCode, request.City, request.CountryCode, request.Region);
        unitOfWork.Tenants.Update(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CompanyDto>.Success(tenant.ToCompanyDto());
    }
}
