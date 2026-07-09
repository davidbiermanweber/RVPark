using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[AdminOnly] // Site + reservation maintenance is admin-only (AccessLevel 3)
public class SitesController : Controller
{
    private readonly AppDbContext _db;

    public SitesController(AppDbContext db) => _db = db;

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
        await ValidateSiteTypeAsync(site);
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", site.CategoryId);
            return View(site);
        }
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
        await ValidateSiteTypeAsync(site);
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", site.CategoryId);
            return View(site);
        }
        _db.Sites.Update(site);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Every site must reference an existing site type.
    private async Task ValidateSiteTypeAsync(Site site)
    {
        if (site.CategoryId <= 0 || !await _db.Categories.AnyAsync(c => c.Id == site.CategoryId))
        {
            ModelState.AddModelError(nameof(site.CategoryId), "Please select a site type.");
        }
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

    // =========================================================================
    // ⚙️ ADDITION: Step 5 - Edit Existing Reservations Admin Actions (Fixed)
    // =========================================================================

    // 1. Master List & Search Panel
    public async Task<IActionResult> ManageReservations(string searchString)
    {
        var query = _db.Reservations
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(r => r.Id.ToString() == searchString ||
                                     r.User.Name.Contains(searchString) ||
                                     r.User.Email.Contains(searchString));
        }

        ViewBag.SearchString = searchString;

        // Maps available site text in-memory for the view layout grid
        ViewBag.SitesMap = await _db.Sites.ToDictionaryAsync(s => s.Id, s => s.Name);

        return View(await query.ToListAsync());
    }

    // 2. Load the Modification Sub-Panel Form
    [HttpGet]
    public async Task<IActionResult> EditReservation(int id)
    {
        var reservation = await _db.Reservations
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        ViewBag.AllSites = await _db.Sites.ToListAsync();
        ViewBag.BalanceMessage = null;

        return View(reservation);
    }

    // 3. Process Live Pricing Shifts, Cancel, & Virtual Availability Validation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReservation(int id, DateTime startDate, DateTime finishDate, int siteId, string statusAction)
    {
        var res = await _db.Reservations.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
        if (res == null) return NotFound();

        ViewBag.AllSites = await _db.Sites.ToListAsync();

        // Handle Cancellation Request Requirements Instantly
        if (statusAction == "Cancel")
        {
            res.ReservationStatus = "Cancelled";
            res.RefundedAmount = res.TotalCost;
            _db.Reservations.Update(res);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageReservations));
        }

        // 💡 FIX: Check site availability constraints cleanly by looking up matching date overlaps
        bool isConflict = await _db.Reservations.AnyAsync(r =>
            r.Id != id &&
            r.ReservationStatus != "Cancelled" &&
            r.StartDate < finishDate &&
            r.FinishDate > startDate);

        if (isConflict)
        {
            ModelState.AddModelError("", "The newly selected campsite dates conflict with an existing active timeline placement.");
            return View(res);
        }

        // Fulfill Step 5 delta requirement: calculate balance differences instead of processing Stripe
        int originalNights = (res.FinishDate - res.StartDate).Days;
        int newNights = (finishDate - startDate).Days;

        if (newNights <= 0)
        {
            ModelState.AddModelError("", "Departure target must happen after arrival.");
            return View(res);
        }

        decimal originalCalculatedCost = originalNights * res.DailyRate;
        decimal newCalculatedCost = newNights * res.DailyRate;
        decimal deltaBalance = newCalculatedCost - originalCalculatedCost;

        // Apply tracking property updates to valid existing object context variables
        res.StartDate = startDate;
        res.FinishDate = finishDate;
        res.TotalCost = newCalculatedCost;

        _db.Reservations.Update(res);
        await _db.SaveChangesAsync();

        ViewBag.BalanceMessage = deltaBalance > 0
            ? $"Changes applied successfully. Additional Payment Required: ${deltaBalance:F2}"
            : $"Changes applied successfully. Refund Calculated: ${Math.Abs(deltaBalance):F2}";

        return View(res);
    }
}