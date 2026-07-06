
public class Fee
{
    public int ID {get;set;}
    public string Name {get; set;} = "";
    public decimal Amount {get;set;}
    public ICollection<ReservationFee>? ReservationFees { get; set; }
}