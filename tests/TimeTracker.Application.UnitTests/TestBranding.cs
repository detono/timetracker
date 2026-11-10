using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.Application.UnitTests;

public record TestBranding(string Title = "Shiftlog") : IAppBranding;
