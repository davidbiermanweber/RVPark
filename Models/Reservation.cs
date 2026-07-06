public class Reservation
{
    public int Id { get; set; }

    // FK → User
    public int UserId { get; set; }
    public User User { get; set; }

    // FK → Status lookup table (recommended)
    public string ReservationStatus {get;set;}

    public decimal TotalCost { get; set; }
    public decimal RefundedAmount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime FinishDate { get; set; }

    public int RvLength { get; set; }

    public decimal DailyRate { get; set; }

    public decimal PriceModifier { get; set; }

    // Fees (you already have this concept)
    public ICollection<ReservationFee>? ReservationFees { get; set; }
}