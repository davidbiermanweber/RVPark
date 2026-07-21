// Self-reported military affiliation for the authorized-patron policy (SYS4).
public enum MilitaryAffiliation
{
    ActiveDuty,
    Reserve,
    NationalGuard,
    Retired,
    Veteran,
    DoDCivilian,
    Dependent,
    Other
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // --- Auth / eligibility (Epic 1). All nullable so existing staff-created guest
    // records (Name/Email/Phone only) remain valid; a value is set on self-registration. ---

    // Hashed password (PasswordHasher). Null = account cannot sign in (no credentials).
    public string? PasswordHash { get; set; }

    // Self-reported affiliation + free-text status (e.g. rank/unit). Required to book (SYS4).
    public MilitaryAffiliation? Affiliation { get; set; }
    public string? MilitaryStatus { get; set; }

    // Email verification: account is inactive until the emailed link is confirmed (G1).
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? TokenExpiresUtc { get; set; }

    // Password reset (G2).
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetExpiresUtc { get; set; }
}
