using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[AdminOnly]
public class FeeController : Controller
{
    private readonly AppDbContext _context;

    public FeeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Fees.ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Fee fee)
    {
        if (ModelState.IsValid)
        {
            _context.Fees.Add(fee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(fee);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var fee = await _context.Fees.FindAsync(id);
        return View(fee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Fee fee)
    {
        _context.Update(fee);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var fee = await _context.Fees.FindAsync(id);
        _context.Fees.Remove(fee);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
