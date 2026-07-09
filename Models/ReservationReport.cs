namespace RvParkApp.Models
{
    public class ReservationReportViewModel
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public List<Reservation> Completed { get; set; } = new();

        public List<Reservation> InProgress { get; set; } = new();

        public List<Reservation> Upcoming { get; set; } = new();
    }
}