using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
    
}