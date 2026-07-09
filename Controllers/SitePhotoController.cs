using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class SitePhotosController : Controller
{
    private readonly AppDbContext _db;
    private readonly BlobContainerClient _container;

    public SitePhotosController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _container = new BlobContainerClient(
            config["AzureStorage:ConnectionString"],
            config["AzureStorage:ContainerName"]);
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

        var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
        var blobClient = _container.GetBlobClient(fileName);

        using (var stream = photo.OpenReadStream())
            await blobClient.UploadAsync(stream, overwrite: true);

        _db.SitePhotos.Add(new SitePhoto { SiteId = siteId, FileName = blobClient.Uri.ToString() });
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
            var blobClient = new BlobClient(new Uri(photo.FileName));
            await blobClient.DeleteIfExistsAsync();

            _db.SitePhotos.Remove(photo);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { siteId });
    }
}
