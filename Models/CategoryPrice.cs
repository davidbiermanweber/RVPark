using System.ComponentModel.DataAnnotations;

// What kind of rate a CategoryPrice row is (A2). Standard is the everyday rate;
// Event/Holiday cover special periods; Premium exists as capability but stays
// unused for now (a Want, not a Must).
public enum PriceType
{
    Standard = 0,
    Event = 1,
    Holiday = 2,
    Premium = 3
}

// A dated price for a site type (Category).
// If EndDate is null, this is the current price for the site type.
public class CategoryPrice
{
    public int Id { get; set; }

    // FK -> Category (site type)
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime StartDate { get; set; }

    // Null = current price (no end date)
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? EndDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater.")]
    public decimal Price { get; set; }

    // Rate kind (A2). Existing rows default to Standard.
    [Display(Name = "Rate type")]
    public PriceType PriceType { get; set; } = PriceType.Standard;

    // Optional admin-facing note, e.g. "Air Show weekend" or "Oct 1 increase".
    [StringLength(100)]
    public string? Label { get; set; }
}
