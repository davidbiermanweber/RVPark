using System.ComponentModel.DataAnnotations;

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
}
