using System.ComponentModel.DataAnnotations;

// Single-row settings table holding the park's reservation rules (A3) and
// cancellation policy (A4). Seeded with the requirement defaults; admins edit it
// on the Park Policies screen. Enforcement (SYS1/SYS2, booking window, fees at
// cancellation time) reads from here — future booking work should snapshot any
// applied fee onto the reservation/cancellation record so later policy edits
// don't rewrite history.
public class ParkPolicy
{
    public int Id { get; set; }

    // ---- Stay rules (A3) ----

    // How far ahead a reservation may be booked.
    [Range(1, 24)]
    [Display(Name = "Booking window (months)")]
    public int BookingWindowMonths { get; set; } = 6;

    // Peak season, inclusive month range (Apr–Oct by default).
    [Range(1, 12)]
    [Display(Name = "Peak season start month")]
    public int PeakStartMonth { get; set; } = 4;

    [Range(1, 12)]
    [Display(Name = "Peak season end month")]
    public int PeakEndMonth { get; set; } = 10;

    // Maximum consecutive-night stay during peak season (SYS1).
    [Range(1, 365)]
    [Display(Name = "Peak max stay (days)")]
    public int PeakMaxStayDays { get; set; } = 14;

    // Long-term winter stay window (Oct 15 – Apr 1 by default).
    [Range(1, 12)]
    [Display(Name = "Long-term window start month")]
    public int LongTermStartMonth { get; set; } = 10;

    [Range(1, 31)]
    [Display(Name = "Long-term window start day")]
    public int LongTermStartDay { get; set; } = 15;

    [Range(1, 12)]
    [Display(Name = "Long-term window end month")]
    public int LongTermEndMonth { get; set; } = 4;

    [Range(1, 31)]
    [Display(Name = "Long-term window end day")]
    public int LongTermEndDay { get; set; } = 1;

    // Days a guest must stay away before returning after a max-length stay (SYS2).
    [Range(0, 365)]
    [Display(Name = "Required days away before returning")]
    public int AwayBeforeReturnDays { get; set; } = 14;

    // ---- Cancellation policy (A4) ----

    // Flat fee for a standard (timely) cancellation.
    [Range(0, 10000)]
    [DataType(DataType.Currency)]
    [Display(Name = "Standard cancellation fee")]
    public decimal CancellationFee { get; set; } = 10.00m;

    // Cancelling fewer than this many days before arrival counts as late.
    [Range(0, 365)]
    [Display(Name = "Late-cancellation threshold (days before arrival)")]
    public int CancellationThresholdDays { get; set; } = 3;

    // Late or holiday-period cancellations are charged one night instead of the flat fee.
    [Display(Name = "Late/holiday cancellation charges one night")]
    public bool LateCancelChargesOneNight { get; set; } = true;
}
