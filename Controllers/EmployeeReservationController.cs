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
        reservation.CancelledAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Reservation #{reservation.Id} was cancelled.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // Walk-In Reservation Process
    //
    // Lives here rather than on SitesController so front-desk employees can
    // actually use it. SitesController is [AdminOnly] (AccessLevel 3), which
    // meant the "Process Walk-In" button on this page 403'd for the very staff
    // it was built for. [EmployeeOnly] still admits admins, since they carry the
    // same Role=Employee claim.
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> WalkIn()
    {
        ViewBag.AllSites = await _context.Sites.Include(s => s.Category).Where(s => s.IsActive).ToListAsync();
        return View(new Reservation
        {
            StartDate = DateTime.Today,
            FinishDate = DateTime.Today.AddDays(1),
            RvLength = 30
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WalkIn(string customerName, string customerEmail, string customerPhone, int siteId, DateTime startDate, DateTime finishDate, int rvLength, bool registerNew = false)
    {
        ViewBag.AllSites = await _context.Sites.Include(s => s.Category).Where(s => s.IsActive).ToListAsync();

        if (finishDate <= startDate)
        {
            ModelState.AddModelError("", "Departure must be after arrival.");
            return View();
        }

        if (!await _availability.IsSiteAvailableAsync(siteId, startDate, finishDate))
        {
            ModelState.AddModelError("", "The selected site is not available for those dates.");
            return View();
        }

        var trimmedEmail = customerEmail?.Trim().ToLower();
        var trimmedPhone = customerPhone?.Trim();

        User? user = null;
        if (!string.IsNullOrEmpty(trimmedEmail))
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == trimmedEmail);
        }
        if (user == null && !string.IsNullOrEmpty(trimmedPhone))
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == trimmedPhone);
        }

        // If user doesn't exist and registration hasn't been confirmed yet
        if (user == null && !registerNew)
        {
            // Pass values back to repopulate the form and trigger the confirmation pop-up
            ViewBag.TriggerUserPrompt = true;
            ViewBag.CustomerName = customerName;
            ViewBag.CustomerEmail = customerEmail;
            ViewBag.CustomerPhone = customerPhone;
            ViewBag.SiteId = siteId;
            ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.FinishDate = finishDate.ToString("yyyy-MM-dd");
            ViewBag.RvLength = rvLength;

            ModelState.AddModelError("", "Customer not found in the system.");
            return View();
        }

        // If user didn't exist but registration was confirmed, create the new user record
        if (user == null)
        {
            var fallbackEmail = string.IsNullOrEmpty(trimmedEmail) ? $"{Guid.NewGuid().ToString().Substring(0, 8)}@walkin.local" : trimmedEmail;
            user = new User
            {
                Name = string.IsNullOrWhiteSpace(customerName) ? "Walk-In Guest" : customerName.Trim(),
                Email = fallbackEmail,
                Phone = trimmedPhone ?? string.Empty
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Fetch site and calculate pricing using category pricing
        var site = await _context.Sites.FindAsync(siteId);
        var today = DateTime.Today;
        var categoryPrice = await _context.CategoryPrices
            .Where(p => p.CategoryId == site.CategoryId && p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        decimal dailyRate = categoryPrice?.Price ?? 0m;
        int nights = Math.Max(1, (finishDate - startDate).Days);

        var reservation = new Reservation
        {
            UserId = user.Id,
            SiteId = siteId,
            StartDate = startDate,
            FinishDate = finishDate,
            RvLength = rvLength,
            ReservationStatus = "Active",
            DailyRate = dailyRate,
            TotalCost = nights * dailyRate,
            RefundedAmount = 0m,
            PriceModifier = 0m
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Walk-in reservation #{reservation.Id} successfully created for {user.Name}!";
        return RedirectToAction(nameof(Index));
    }
}