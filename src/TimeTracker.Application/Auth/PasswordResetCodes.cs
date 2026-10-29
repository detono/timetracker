using System.Security.Cryptography;

namespace TimeTracker.Application.Auth;

/// <summary>
/// Codes like <c>K7QM-3XTP</c>: 8 characters from an alphabet without look-alikes (no 0/O,
/// 1/I/L), about 39 bits. Together with the expiry and the attempt limit that's far beyond
/// guessing, and short enough to type over from an email.
/// </summary>
public static class PasswordResetCodes
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Length = 8;

    public static string Generate()
    {
        var chars = RandomNumberGenerator.GetItems<char>(Alphabet, Length);
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
    }

    /// <summary>Forgives case, spaces and dashes, so "k7qm 3xtp" matches "K7QM-3XTP".</summary>
    public static string Normalize(string code) =>
        new(code.Where(char.IsAsciiLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
