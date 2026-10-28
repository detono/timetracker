using Microsoft.AspNetCore.DataProtection;
using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.Infrastructure.Email;

/// <summary>Encrypts stored secrets with ASP.NET Core Data Protection (key ring persisted in the database).</summary>
public class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector {
    // Changing this string makes every stored secret unreadable, so don't.
    private const string Purpose = "TimeTracker.MailSettings.Secrets.v1";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
