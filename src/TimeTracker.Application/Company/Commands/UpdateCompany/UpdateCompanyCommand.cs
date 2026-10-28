using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Company.Dtos;

namespace TimeTracker.Application.Company.Commands.UpdateCompany;

/// <summary>Employer-only: renames the company and sets its address.</summary>
public record UpdateCompanyCommand(
    string Name,
    string? Street,
    string? PostalCode,
    string? City,
    string? CountryCode,
    string? Region) : IRequest<Result<CompanyDto>>;
