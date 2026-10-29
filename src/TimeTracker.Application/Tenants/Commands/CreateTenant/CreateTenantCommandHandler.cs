using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Tenants.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher
) : IRequestHandler<CreateTenantCommand, Result<TenantDto>> {
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result<TenantDto>.Failure("Only a platform admin can create tenants.", ResultErrorType.Forbidden);
        }

        if (await unitOfWork.Tenants.NameExistsAsync(request.Name, cancellationToken)) {
            return Result<TenantDto>.Failure("A tenant with this name already exists.", ResultErrorType.Conflict);
        }

        if (await unitOfWork.Users.EmailExistsAsync(request.EmployerEmail, cancellationToken)) {
            return Result<TenantDto>.Failure("A user with this email already exists.", ResultErrorType.Conflict);
        }

        var tenant = Tenant.Create(request.Name);
        var employer = User.Create(
            request.EmployerFirstName,
            request.EmployerLastName,
            request.EmployerEmail,
            passwordHasher.Hash(request.EmployerPassword),
            UserRole.Employer,
            tenant.Id);
        employer.RequirePasswordChange();

        await unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
        await unitOfWork.Users.AddAsync(employer, cancellationToken);
        foreach (var hourType in DefaultHourTypes.CreateFor(tenant.Id)) {
            await unitOfWork.HourTypes.AddAsync(hourType, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantDto>.Success(new TenantDto(tenant.Id, tenant.Name, tenant.IsActive, 1, tenant.CreatedAtUtc));
    }
}
