using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Auth.Commands.SetLanguage;

/// <summary>Saves the caller's language (two letters, e.g. "de"); null clears it.</summary>
public record SetLanguageCommand(string? Language) : IRequest<Result>;
