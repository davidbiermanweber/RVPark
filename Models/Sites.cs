using System.ComponentModel.DataAnnotations;

public class Site
{
    public int Id {get; set;}

    [Required(ErrorMessage = "Site name is required.")]
    [StringLength(100, ErrorMessage = "Site name cannot be longer than 100 characters.")]
    [Display(Name = "Site Name")]
    public string Name {get; set; } = string.Empty;

    // Genuinely optional — Home/Index falls back to placeholder copy when it's blank.
    // Nullable reference types make a non-nullable string implicitly [Required], which
    // would block editing the existing rows that already have no description, so opt
    // out explicitly rather than leaving the column NOT NULL and the form unsatisfiable.
    // ConvertEmptyStringToNull is on by default, so an empty box binds to null and trips
    // Required regardless of AllowEmptyStrings — both attributes are needed to opt out.
    [Required(AllowEmptyStrings = true)]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
    public string Description {get; set; } = string.Empty;

    // 0 is what an unselected dropdown posts, so Range (not Required) is what
    // actually catches "no site type chosen".
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a site type.")]
    [Display(Name = "Site Type")]
    public int CategoryId {get; set;}

    public Category? Category {get; set;}

    // Longest RV (in feet) this site can accommodate. Feeds availability search
    // results (G3) and the RV-length matching logic (G4). 0 = unspecified.
    [Range(0, 120, ErrorMessage = "Max RV length must be between 0 and 120 feet (0 = unspecified).")]
    [Display(Name = "Max RV Length")]
    public int MaxRvLength { get; set; }

    // Indefinite availability toggle (e.g. permanently out of service). Date-ranged
    // maintenance blocks live in SiteBlock; this is the "always off" switch.
    public bool IsActive { get; set; } = true;

    public List<SitePhoto> Photos { get; set; } = new();

    // Maintenance / special-use blocks that make this site unbookable for a period (S2).
    public List<SiteBlock> Blocks { get; set; } = new();
}
