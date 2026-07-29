namespace RvParkApp.Models
{
    public class ReservationReportViewModel
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public List<Reservation> Completed { get; set; } = new();

        public List<Reservation> InProgress { get; set; } = new();

        public List<Reservation> Upcoming { get; set; } = new();

        // Dashboard statistics
        public decimal TotalRevenue { get; set; }

        public int TotalSites { get; set; }

        public int OccupiedSites { get; set; }

        public double OccupancyRate { get; set; }

        // Daily reports
        public List<Reservation> ArrivalsToday { get; set; } = new();

        public List<Reservation> DeparturesToday { get; set; } = new();
    }
}