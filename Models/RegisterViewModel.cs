using System.ComponentModel.DataAnnotations;

// Input model for customer self-registration (G1). Validation lives here so the
// User entity stays free of form-only concerns like password confirmation.
public class RegisterViewModel
{
    [Required]
    [Display(Name = "Full name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Military affiliation")]
    public MilitaryAffiliation? Affiliation { get; set; }

    [Display(Name = "Status / rank / unit (optional)")]
    public string? MilitaryStatus { get; set; }
}
