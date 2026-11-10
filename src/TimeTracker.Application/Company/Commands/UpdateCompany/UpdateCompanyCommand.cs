using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Company.Dtos;

namespace TimeTracker.Application.Company.Commands.UpdateCompany;

/// <summary>
/// Employer-only: renames the company and sets its address, and whether employers get a copy
/// of leave requests a supervisor handles (left out: unchanged).
/// </summary>
public record UpdateCompanyCommand(
    string Name,
    string? Street,
    string? PostalCode,
    string? City,
    string? CountryCode,
    string? Region,
    bool? CopyEmployersOnLeaveRequests = null) : IRequest<Result<CompanyDto>>;
