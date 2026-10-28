namespace TimeTracker.Application.Common.Interfaces;

/// <summary>Encrypts secrets (passwords, client secrets) before they're stored, and decrypts them for use.</summary>
public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
