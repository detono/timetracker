using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Company.Dtos;

namespace TimeTracker.Application.Company.Queries.GetCompany;

/// <summary>The caller's own company details. Employer only.</summary>
public record GetCompanyQuery : IRequest<Result<CompanyDto>>;
