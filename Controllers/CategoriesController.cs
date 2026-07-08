using System.Formats.Asn1;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .Include(c => c.Prices)
            .ToListAsync();
        return View(categories);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid)
            return View(category);

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Combined "manage site type" page: name + its price ranges.
    // editPriceId, when set, switches that price row into inline-edit mode.
    public async Task<IActionResult> Edit(int id, int? editPriceId)
    {
        var category = await _db.Categories
            .Include(c => c.Prices)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();

        ViewBag.EditPriceId = editPriceId;
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Category category)
    {
        var existing = await _db.Categories
            .Include(c => c.Prices)
            .FirstOrDefaultAsync(c => c.Id == category.Id);
        if (existing == null) return NotFound();

        if (!ModelState.IsValid) return View(existing);

        // Only the name is edited here; prices are managed via CategoryPricesController.
        existing.Name = category.Name;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category != null)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}