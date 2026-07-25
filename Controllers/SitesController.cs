using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[AdminOnly] // Site + reservation maintenance is admin-only (AccessLevel 3)
public class SitesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAvailabilityService _availability;

    public SitesController(AppDbContext db, IAvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    public async Task<IActionResult> Index()
    {
        var sites = await _db.Sites
        .Include(s => s.Category)
        .Include(s => s.Photos)
        .ToListAsync();
        return View(sites);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Site site)
    {
        if (!ModelState.IsValid) return View(site);
        _db.Sites.Add(site);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var site = await _db.Sites.FindAsync(id);
        if (site == null) return NotFound();
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
        return View(site);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Site site)
    {
        if (!ModelState.IsValid) return View(site);
        _db.Sites.Update(site);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var site = await _db.Sites.FindAsync(id);
        if (site != null)
        {
            _db.Sites.Remove(site);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // Admin availability grid: sites × dates showing Open / Reserved / Blocked. Loads
    // reservations and blocks once for the window and computes cells in memory.
    [HttpGet]
    public async Task<IActionResult> Availability(DateTime? start, int days = 14)
    {
        var startDate = (start ?? DateTime.Today).Date;
        days = Math.Clamp(days, 1, 60);
        var endDate = startDate.AddDays(days);

        var sites = await _db.Sites
            .Include(s => s.Category)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var reservations = await _db.Reservations
            .Where(r => r.SiteId != null && !r.ReservationStatus.Contains("Cancelled")
                        && r.StartDate < endDate && startDate < r.FinishDate)
            .Select(r => new { SiteId = r.SiteId!.Value, r.StartDate, r.FinishDate })
            .ToListAsync();

        var blocks = await _db.SiteBlocks
            .Where(b => b.StartDate < endDate && startDate < b.EndDate)
            .Select(b => new { b.SiteId, b.StartDate, b.EndDate })
            .ToListAsync();

        var dates = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();

        var vm = new AvailabilityGridViewModel
        {
            StartDate = startDate,
            Days = days,
            Dates = dates
        };

        foreach (var site in sites)
        {
            var row = new AvailabilityGridViewModel.SiteRow
            {
                SiteId = site.Id,
                Name = site.Name,
                Category = site.Category?.Name ?? "",
                IsActive = site.IsActive
            };

            foreach (var day in dates)
            {
                var dayEnd = day.AddDays(1);
                if (!site.IsActive)
                    row.Cells.Add("inactive");
                else if (blocks.Any(b => b.SiteId == site.Id && b.StartDate < dayEnd && day < b.EndDate))
                    row.Cells.Add("blocked");
                else if (reservations.Any(r => r.SiteId == site.Id && r.StartDate < dayEnd && day < r.FinishDate))
                    row.Cells.Add("reserved");
                else
                    row.Cells.Add("open");
            }

            vm.Sites.Add(row);
        }

        return View(vm);
    }

    // =========================================================================
    // Site availability blocks (S2) — maintenance / special-use windows that make a
    // site unbookable. Replaces the old hack of writing status into Site.Description.
    // =========================================================================

    // List existing blocks for a site + form to add a new one.
    [HttpGet]
    public async Task<IActionResult> Blocks(int siteId)
    {
        var site = await _db.Sites
            .Include(s => s.Blocks)
            .FirstOrDefaultAsync(s => s.Id == siteId);

        if (site == null) return NotFound();
        return View(site);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(int siteId, DateTime startDate, DateTime endDate, string reason)
    {
        var site = await _db.Sites.FindAsync(siteId);
        if (site == null) return NotFound();

        if (endDate <= startDate)
            TempData["BlockError"] = "End date must be after start date.";
        else if (string.IsNullOrWhiteSpace(reason))
            TempData["BlockError"] = "A reason is required.";
        else
        {
            _db.SiteBlocks.Add(new SiteBlock
            {
                SiteId = siteId,
                StartDate = startDate,
                EndDate = endDate,
                Reason = reason.Trim(),
                CreatedByEmployeeId = await CurrentEmployeeIdAsync(),
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Blocks), new { siteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int id, int siteId)
    {
        var block = await _db.SiteBlocks.FindAsync(id);
        if (block != null)
        {
            _db.SiteBlocks.Remove(block);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Blocks), new { siteId });
    }

    // Resolves the logged-in employee's Id from the username claim (audit — NFR-11).
    private async Task<int?> CurrentEmployeeIdAsync()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return null;
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Username == username);
        return emp?.Id;
    }

    // =========================================================================
    // Edit Existing Reservations — admin actions. Uses the real Reservation.SiteId
    // FK and IAvailabilityService instead of encoding site/status into strings.
    // =========================================================================

    // Master list & search panel.
    public async Task<IActionResult> ManageReservations(string searchString)
    {
        var query = _db.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .Include(r => r.ReservationFees)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(r => r.Id.ToString() == searchString ||
                                     r.User.Name.Contains(searchString) ||
                                     r.User.Email.Contains(searchString));
        }

        ViewBag.SearchString = searchString;
        return View(await query.ToListAsync());
    }

    // Load the modification form.
    [HttpGet]
    public async Task<IActionResult> EditReservation(int id)
    {
        var reservation = await _db.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .Include(r => r.ReservationFees)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        ViewBag.AllSites = await _db.Sites.Include(s => s.Category).ToListAsync();
        ViewBag.BalanceMessage = null;

        return View(reservation);
    }

    // Cancel / un-cancel / update (site + dates), with availability checks and a balance delta.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReservation(int id, DateTime startDate, DateTime finishDate, int siteId, string statusAction)
    {
        var res = await _db.Reservations
            .Include(r => r.User)
            .Include(r => r.Site)
            .Include(r => r.ReservationFees)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (res == null) return NotFound();

        ViewBag.AllSites = await _db.Sites.Include(s => s.Category).ToListAsync();

        if (statusAction == "Cancel")
        {
            res.ReservationStatus = "Cancelled";
            res.RefundedAmount = res.TotalCost;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageReservations));
        }

        if (statusAction == "UnCancel")
        {
            if (res.SiteId == null ||
                !await _availability.IsSiteAvailableAsync(res.SiteId.Value, res.StartDate, res.FinishDate, ignoreReservationId: res.Id))
            {
                ModelState.AddModelError("", "Cannot un-cancel: assign an available site for those dates first.");
                return View(res);
            }

            res.ReservationStatus = "Active";
            res.RefundedAmount = 0m;
            await _db.SaveChangesAsync();

            ViewBag.BalanceMessage = "Reservation successfully reactivated to Active status.";
            return View(res);
        }

        // Update: validate dates + availability, then recalculate the balance delta.
        if (finishDate <= startDate)
        {
            ModelState.AddModelError("", "Departure must be after arrival.");
            return View(res);
        }

        if (!await _availability.IsSiteAvailableAsync(siteId, startDate, finishDate, ignoreReservationId: res.Id))
        {
            ModelState.AddModelError("", "The selected site is not available for those dates.");
            return View(res);
        }

        int originalNights = (res.FinishDate - res.StartDate).Days;
        int newNights = (finishDate - startDate).Days;
        decimal deltaBalance = (newNights - originalNights) * res.DailyRate;

        res.SiteId = siteId;
        res.StartDate = startDate;
        res.FinishDate = finishDate;
        res.TotalCost = newNights * res.DailyRate;
        if (res.ReservationStatus == "Cancelled") res.ReservationStatus = "Active";

        await _db.SaveChangesAsync();

        res.Site = await _db.Sites.FindAsync(siteId);

        ViewBag.BalanceMessage = deltaBalance >= 0
            ? $"Changes applied successfully. Additional payment required: ${deltaBalance:F2}."
            : $"Changes applied successfully. Refund calculated: ${Math.Abs(deltaBalance):F2}.";

        return View(res);
    }

    // =========================================================================
    // Walk-In Reservation Process
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> WalkIn()
    {
        ViewBag.AllSites = await _db.Sites.Include(s => s.Category).Where(s => s.IsActive).ToListAsync();
        return View(new Reservation
        {
            StartDate = DateTime.Today,
            FinishDate = DateTime.Today.AddDays(1),
            RvLength = 30
        });
    }

// JSON endpoint to verify if a user exists by email or phone
    [HttpGet]
    public async Task<IActionResult> CheckUser(string email, string phone)
    {
        var trimmedEmail = email?.Trim().ToLower();
        var trimmedPhone = phone?.Trim();

        User? user = null;
        if (!string.IsNullOrEmpty(trimmedEmail))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == trimmedEmail);
        }
        if (user == null && !string.IsNullOrEmpty(trimmedPhone))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == trimmedPhone);
        }

        return Json(new { exists = user != null, name = user?.Name });
    }

   [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WalkIn(string customerName, string customerEmail, string customerPhone, int siteId, DateTime startDate, DateTime finishDate, int rvLength, bool registerNew = false)
    {
        ViewBag.AllSites = await _db.Sites.Include(s => s.Category).Where(s => s.IsActive).ToListAsync();

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
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == trimmedEmail);
        }
        if (user == null && !string.IsNullOrEmpty(trimmedPhone))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == trimmedPhone);
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
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // Fetch site and calculate pricing using category pricing
        var site = await _db.Sites.FindAsync(siteId);
        var today = DateTime.Today;
        var categoryPrice = await _db.CategoryPrices
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

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Walk-in reservation #{reservation.Id} successfully created for {user.Name}!";
        return RedirectToAction(nameof(ManageReservations));
    }
}