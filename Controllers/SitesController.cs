using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class SitesController : Controller
{
    private readonly AppDbContext _db;

    public SitesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var sites = await _db.Sites.Include(s => s.Category).ToListAsync();
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
}