// View model for the admin availability grid (sites × dates). Each cell carries a
// status string ("open" | "reserved" | "blocked" | "inactive") for CSS styling.
public class AvailabilityGridViewModel
{
    public DateTime StartDate { get; set; }
    public int Days { get; set; }
    public List<DateTime> Dates { get; set; } = new();
    public List<SiteRow> Sites { get; set; } = new();

    public class SiteRow
    {
        public int SiteId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Cells { get; set; } = new(); // one per date, same order as Dates
    }
}
