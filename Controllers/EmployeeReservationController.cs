using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RvParkApp.Models;

[EmployeeOnly]
public class EmployeeReservationController : Controller
{
    private readonly AppDbContext _context;
    private readonly IAvailabilityService _availability;

    public EmployeeReservationController(
        AppDbContext context,
        IAvailabilityService availability)
    {
        _context = context;
        _availability = availability;
    }

    // Displays all reservations.
    // Employees can search by customer/site and filter by status.
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status)
    {
        var query = _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(r =>
                r.User.Name.Contains(search) ||
                r.User.Email.Contains(search) ||
                r.User.Phone.Contains(search) ||
                (r.Site != null &&
                 r.Site.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r =>
                r.ReservationStatus == status);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;

        var reservations = await query
            .OrderBy(r => r.StartDate)
            .ToListAsync();

        return View(reservations);
    }

    // Displays one reservation in detail.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .Include(r => r.ReservationFees)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        return View(reservation);
    }

    // Displays the edit reservation form.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        return View(reservation);
    }

    // Saves changes to an existing reservation.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        DateTime startDate,
        DateTime finishDate,
        int rvLength)
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        // Validate dates.
        if (finishDate <= startDate)
        {
            ModelState.AddModelError(
                "",
                "Departure date must be after the arrival date.");
        }

        if (startDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                "",
                "The arrival date cannot be in the past.");
        }

        // Validate RV length.
        if (rvLength <= 0)
        {
            ModelState.AddModelError(
                "",
                "RV length must be greater than zero.");
        }

        // Check whether the RV fits the assigned site.
        if (reservation.Site != null &&
            reservation.Site.MaxRvLength > 0 &&
            rvLength > reservation.Site.MaxRvLength)
        {
            ModelState.AddModelError(
                "",
                $"This site supports RVs up to " +
                $"{reservation.Site.MaxRvLength} feet.");
        }

        // Perform a fresh availability check.
    
        if (ModelState.IsValid &&
            reservation.SiteId.HasValue)
        {
            bool available =
                await _availability.IsSiteAvailableAsync(
                    reservation.SiteId.Value,
                    startDate,
                    finishDate,
                    reservation.Id);

            if (!available)
            {
                ModelState.AddModelError(
                    "",
                    "The assigned site is not available for " +
                    "the selected dates. Another reservation " +
                    "or maintenance block conflicts with this change.");
            }
        }

        if (!ModelState.IsValid)
        {
            reservation.StartDate = startDate;
            reservation.FinishDate = finishDate;
            reservation.RvLength = rvLength;

            return View(reservation);
        }

        // Save reservation changes.
        reservation.StartDate = startDate;
        reservation.FinishDate = finishDate;
        reservation.RvLength = rvLength;

        
        int nights =
        (finishDate.Date - startDate.Date).Days;

        reservation.TotalCost =
            nights * reservation.DailyRate;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Reservation #{reservation.Id} was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // Cancels a reservation without deleting its history.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var reservation =
            await _context.Reservations.FindAsync(id);

        if (reservation == null)
        {
            return NotFound();
        }

        if (reservation.ReservationStatus == "Cancelled")
        {
            TempData["ErrorMessage"] =
                "This reservation is already cancelled.";

            return RedirectToAction(nameof(Index));
        }

        reservation.ReservationStatus = "Cancelled";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Reservation #{reservation.Id} was cancelled.";

        return RedirectToAction(nameof(Index));
    }
}