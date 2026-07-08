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
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Index(DateTime startDate, DateTime endDate)
    {
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
                .Where(r => r.ReservationStatus == "Completed")
                .ToList(),

            InProgress = reservations
                .Where(r => r.ReservationStatus == "In Progress")
                .ToList(),

            Upcoming = reservations
                .Where(r => r.ReservationStatus == "Upcoming")
                .ToList()
        };


        return View(model);
    }
}