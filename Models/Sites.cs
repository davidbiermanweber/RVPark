public class Site
{
    public int Id {get; set;}
    public string Name {get; set; } = string.Empty;
    public string Description {get; set; } = string.Empty;
    public int CategoryId {get; set;}
    public Category? Category {get; set;}

    // Longest RV (in feet) this site can accommodate. Feeds availability search
    // results (G3) and the RV-length matching logic (G4). 0 = unspecified.
    public int MaxRvLength { get; set; }

    // Indefinite availability toggle (e.g. permanently out of service). Date-ranged
    // maintenance blocks live in SiteBlock; this is the "always off" switch.
    public bool IsActive { get; set; } = true;

    public List<SitePhoto> Photos { get; set; } = new();

    // Maintenance / special-use blocks that make this site unbookable for a period (S2).
    public List<SiteBlock> Blocks { get; set; } = new();
}