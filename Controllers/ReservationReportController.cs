using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RvParkApp.Models;

public class ReservationReportController : Controller
{
    private readonly AppDbContext _context;

    public ReservationReportController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(new ReservationReportViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(DateTime startDate, DateTime endDate)
    {
        // Optional validation
        if (endDate < startDate)
        {
            ModelState.AddModelError("", "End date must be on or after the start date.");
            return View();
        }

        var today = DateTime.Today;

        var reservations = await _context.Reservations
            .Include(r => r.User)
            .Where(r => r.StartDate <= endDate &&
                        r.FinishDate >= startDate)
            .ToListAsync();

        var model = new ReservationReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,

            Completed = reservations
                .Where(r => r.FinishDate < today &&
                            r.ReservationStatus != "Cancelled")
                .OrderBy(r => r.FinishDate)
                .ToList(),

            InProgress = reservations
                .Where(r => r.StartDate <= today &&
                            r.FinishDate >= today &&
                            r.ReservationStatus != "Cancelled")
                .OrderBy(r => r.StartDate)
                .ToList(),

            Upcoming = reservations
                .Where(r => r.StartDate > today &&
                            r.ReservationStatus != "Cancelled")
                .OrderBy(r => r.StartDate)
                .ToList()
        };

        // Dashboard Statistics

        // Ignore cancelled reservations in revenue
        model.TotalRevenue = reservations
            .Where(r => r.ReservationStatus != "Cancelled")
            .Sum(r => r.TotalCost);

        model.TotalSites = await _context.Sites.CountAsync();

        model.OccupiedSites = reservations
            .Where(r =>
                r.StartDate <= today &&
                r.FinishDate >= today &&
                r.ReservationStatus != "Cancelled" &&
                r.SiteId.HasValue)
            .Select(r => r.SiteId)
            .Distinct()
            .Count();

        model.OccupancyRate =
            model.TotalSites == 0
                ? 0
                : (double)model.OccupiedSites / model.TotalSites * 100;

        // Daily Reports

        model.ArrivalsToday = reservations
            .Where(r => r.StartDate.Date == today &&
                        r.ReservationStatus != "Cancelled")
            .OrderBy(r => r.StartDate)
            .ToList();

        model.DeparturesToday = reservations
            .Where(r => r.FinishDate.Date == today &&
                        r.ReservationStatus != "Cancelled")
            .OrderBy(r => r.FinishDate)
            .ToList();

        return View(model);
    }
}