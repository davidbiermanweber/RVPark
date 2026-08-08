using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RvParkApp.Models;

namespace RvParkApp.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Fetch active sites with categories and photos
        var sites = await _db.Sites
            .Include(s => s.Category)
            .Include(s => s.Photos)
            .Where(s => s.IsActive)
            .ToListAsync();

        // Fetch current category pricing to display rates
        var rates = await _db.CategoryPrices
            .Include(cp => cp.Category)
            .Where(cp => cp.EndDate == null || cp.EndDate >= DateTime.Today)
            .ToListAsync();

        ViewBag.Rates = rates;

        return View(sites);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}