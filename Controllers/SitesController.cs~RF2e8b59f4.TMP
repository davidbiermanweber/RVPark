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
        if(!ModelState.IsValid) return View(site);
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
        if(!ModelState.IsValid) return View(site);
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
}