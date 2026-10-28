using MediatR;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PublicHolidays.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PublicHolidays.Commands.ImportPublicHolidays;

public class ImportPublicHolidaysCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IPublicHolidayProvider provider
) : IRequestHandler<ImportPublicHolidaysCommand, Result<PublicHolidayImportDto>> {
    public async Task<Result<PublicHolidayImportDto>> Handle(ImportPublicHolidaysCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer || currentUser.TenantId is not { } tenantId) {
            return Result<PublicHolidayImportDto>.Failure("Only an employer can manage holidays.", ResultErrorType.Forbidden);
        }

        if (request.Year is < 2000 or > 2100) {
            return Result<PublicHolidayImportDto>.Failure("The year is out of range.");
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant?.CountryCode is not { } country) {
            return Result<PublicHolidayImportDto>.Failure("Set the company's country first.");
        }

        IReadOnlyList<ExternalPublicHoliday> official;
        try {
            official = await provider.GetAsync(country, request.Year, cancellationToken);
        } catch (PublicHolidayLookupException ex) {
            return Result<PublicHolidayImportDto>.Failure(ex.Message);
        }

        var from = new DateOnly(request.Year, 1, 1);
        var to = new DateOnly(request.Year, 12, 31);
        var existingDates = (await unitOfWork.PublicHolidays.GetBetweenAsync(from, to, cancellationToken))
            .Select(h => h.Date)
            .ToHashSet();

        var toAdd = official
            .Where(h => h.Regions is null || (tenant.Region is not null && h.Regions.Contains(tenant.Region)))
            .Where(h => h.Date.Year == request.Year && existingDates.Add(h.Date))
            .ToList();

        foreach (var holiday in toAdd) {
            await unitOfWork.PublicHolidays.AddAsync(PublicHoliday.Create(holiday.Date, holiday.Name), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var all = await unitOfWork.PublicHolidays.GetBetweenAsync(from, to, cancellationToken);
        return Result<PublicHolidayImportDto>.Success(new PublicHolidayImportDto(
            toAdd.Count, all.Select(h => new PublicHolidayDto(h.Id, h.Date, h.Name)).ToList()));
    }
}
