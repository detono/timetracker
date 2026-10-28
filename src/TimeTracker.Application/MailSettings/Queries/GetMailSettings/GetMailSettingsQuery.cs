using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.MailSettings.Dtos;

namespace TimeTracker.Application.MailSettings.Queries.GetMailSettings;

/// <summary>Employer-only: the company's mail settings, never including secrets.</summary>
public record GetMailSettingsQuery : IRequest<Result<MailSettingsDto>>;
