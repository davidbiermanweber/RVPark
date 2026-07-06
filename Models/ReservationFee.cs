public class ReservationFee
{
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; }

    public int FeeID { get; set; }
    public Fee Fee { get; set; }

    public decimal AppliedAmount { get; set; }
}
