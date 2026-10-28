using TimeTracker.Application.Company.Dtos;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Company;

internal static class CompanyMapping {
    public static CompanyDto ToCompanyDto(this Tenant tenant) =>
        new(tenant.Name, tenant.Street, tenant.PostalCode, tenant.City, tenant.CountryCode, tenant.Region);
}
