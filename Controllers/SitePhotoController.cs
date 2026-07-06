using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class SitePhotosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SitePhotosController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(int siteId)
    {
        var site = await _db.Sites.FindAsync(siteId);
        if (site == null) return NotFound();

        var photos = await _db.SitePhotos.Where(p => p.SiteId == siteId).ToListAsync();
        ViewBag.Site = site;
        return View(photos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int siteId, IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
        {
            TempData["Error"] = "Please select a photo before uploading.";
            return RedirectToAction(nameof(Index), new { siteId });
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "sites");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await photo.CopyToAsync(stream);

        _db.SitePhotos.Add(new SitePhoto { SiteId = siteId, FileName = fileName });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { siteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int siteId)
    {
        var photo = await _db.SitePhotos.FindAsync(id);
        if (photo != null)
        {
            var filePath = Path.Combine(_env.WebRootPath, "images", "sites", photo.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            
            _db.SitePhotos.Remove(photo);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new {siteId});
    }
}