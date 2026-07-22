using System.ComponentModel.DataAnnotations;

// A date-ranged block that makes a Site unbookable for maintenance or special use (S2).
// Overlap with [StartDate, EndDate) removes the site from availability for that window.
public class SiteBlock
{
    public int Id { get; set; }

    // FK → Site
    public int SiteId { get; set; }
    public Site? Site { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime EndDate { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    // Employee who created the block (audit trail — NFR-11).
    public int? CreatedByEmployeeId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
