using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.Application.UnitTests;

public record TestBranding(
    string Title = "Shiftlog",
    string? AppUrl = null,
    string ColorPrimary = "#123456",
    string ColorOnPrimary = "#ffffff",
    string ColorSecondary = "#abcdef",
    string ColorOnSecondary = "#000000") : IAppBranding;
