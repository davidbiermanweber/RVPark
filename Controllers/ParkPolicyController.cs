using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Admin screen for the park's stay rules (A3) and cancellation policy (A4).
// Edits the single seeded ParkPolicy row.
[AdminOnly]
public class ParkPolicyController : Controller
{
    private readonly AppDbContext _db;

    public ParkPolicyController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        // The row is seeded by migration; the fallback covers a fresh/manually
        // created database that never ran the seed.
        var policy = await _db.ParkPolicies.FirstOrDefaultAsync();
        if (policy == null)
        {
            policy = new ParkPolicy();
            _db.ParkPolicies.Add(policy);
            await _db.SaveChangesAsync();
        }
        return View(policy);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ParkPolicy policy)
    {
        if (!ModelState.IsValid) return View(policy);

        var existing = await _db.ParkPolicies.FindAsync(policy.Id);
        if (existing == null) return NotFound();

        existing.BookingWindowMonths = policy.BookingWindowMonths;
        existing.PeakStartMonth = policy.PeakStartMonth;
        existing.PeakEndMonth = policy.PeakEndMonth;
        existing.PeakMaxStayDays = policy.PeakMaxStayDays;
        existing.LongTermStartMonth = policy.LongTermStartMonth;
        existing.LongTermStartDay = policy.LongTermStartDay;
        existing.LongTermEndMonth = policy.LongTermEndMonth;
        existing.LongTermEndDay = policy.LongTermEndDay;
        existing.AwayBeforeReturnDays = policy.AwayBeforeReturnDays;
        existing.CancellationFee = policy.CancellationFee;
        existing.CancellationThresholdDays = policy.CancellationThresholdDays;
        existing.LateCancelChargesOneNight = policy.LateCancelChargesOneNight;

        await _db.SaveChangesAsync();
        TempData["Saved"] = true;
        return RedirectToAction(nameof(Edit));
    }
}
