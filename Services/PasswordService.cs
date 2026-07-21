using Microsoft.AspNetCore.Identity;

// Password hashing for both customer (User) and employee accounts (NFR-3). Wraps the
// framework PasswordHasher (no extra NuGet). Verify transparently accepts legacy
// plaintext values (e.g. the seeded admin) and flags them so callers can rehash on
// the next successful login ("hash-on-login" upgrade).
public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string? stored, string password, out bool needsUpgrade);
}

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Dummy = new();

    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    public bool Verify(string? stored, string password, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (string.IsNullOrEmpty(stored)) return false;

        if (!LooksHashed(stored))
        {
            // Legacy plaintext: compare directly, flag so the caller rehashes it.
            if (stored == password)
            {
                needsUpgrade = true;
                return true;
            }
            return false;
        }

        var result = _hasher.VerifyHashedPassword(Dummy, stored, password);
        if (result == PasswordVerificationResult.Success) return true;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            needsUpgrade = true;
            return true;
        }
        return false;
    }

    // PasswordHasher output is base64 whose first decoded byte is a version marker
    // (0x00 = v2, 0x01 = v3) and is far longer than any seeded plaintext.
    private static bool LooksHashed(string value)
    {
        if (value.Length < 40) return false;
        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length > 0 && (bytes[0] == 0x00 || bytes[0] == 0x01);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
