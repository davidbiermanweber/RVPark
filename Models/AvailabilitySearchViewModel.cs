// Backs the public availability search page (G3/G9): the search inputs plus results.
public class AvailabilitySearchViewModel
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public int? CategoryId { get; set; }
    public int? RvLength { get; set; }

    public bool Searched { get; set; }
    public int Nights { get; set; }
    public List<Result> Results { get; set; } = new();

    public class Result
    {
        public int SiteId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxRvLength { get; set; }
        public decimal? NightlyPrice { get; set; }
        public decimal? Total => NightlyPrice.HasValue ? NightlyPrice : null;
    }
}
