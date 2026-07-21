using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

// Public, mobile-friendly availability search/browse (G3/G9). No login required to look;
// booking (a later epic) will require an eligible, verified account.
public class AvailabilityController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAvailabilityService _availability;

    public AvailabilityController(AppDbContext db, IAvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    [HttpGet]
    public async Task<IActionResult> Search(DateTime? start, DateTime? end, int? categoryId, int? rvLength)
    {
        await PopulateCategoriesAsync(categoryId);

        var vm = new AvailabilitySearchViewModel
        {
            Start = start,
            End = end,
            CategoryId = categoryId,
            RvLength = rvLength
        };

        // Only run a search once both dates are present.
        if (start.HasValue && end.HasValue)
        {
            if (end.Value <= start.Value)
            {
                ModelState.AddModelError("", "Departure must be after arrival.");
                return View(vm);
            }

            var sites = await _availability.AvailableSitesAsync(start.Value, end.Value, categoryId, rvLength);

            // Current price per category: latest CategoryPrice effective today.
            var today = DateTime.Today;
            var prices = await _db.CategoryPrices
                .Where(p => p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))
                .ToListAsync();

            vm.Nights = (end.Value - start.Value).Days;
            vm.Results = sites.Select(s =>
            {
                var price = prices
                    .Where(p => p.CategoryId == s.CategoryId)
                    .OrderByDescending(p => p.StartDate)
                    .FirstOrDefault();
                return new AvailabilitySearchViewModel.Result
                {
                    SiteId = s.Id,
                    Name = s.Name,
                    Category = s.Category?.Name ?? string.Empty,
                    MaxRvLength = s.MaxRvLength,
                    NightlyPrice = price?.Price
                };
            }).ToList();
            vm.Searched = true;
        }

        return View(vm);
    }

    private async Task PopulateCategoriesAsync(int? selected)
    {
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", selected);
    }
}
