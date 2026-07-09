using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Handles CRUD for a site type's dated prices.
// All actions redirect back to the combined Site Type edit page.
[AdminOnly] // Pricing maintenance is admin-only (AccessLevel 3)
public class CategoryPricesController : Controller
{
    private readonly AppDbContext _db;

    public CategoryPricesController(AppDbContext db) => _db = db;

    // NOTE: the parameter is named 'categoryPrice', NOT 'price'. A parameter named
    // 'price' collides with the posted 'Price' field: the binder treats 'price' as a
    // prefix, so the bare form fields (CategoryId, StartDate, ...) fail to bind.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryPrice categoryPrice)
    {
        if (ModelState.IsValid)
        {
            _db.CategoryPrices.Add(categoryPrice);
            await _db.SaveChangesAsync();
        }
        else
        {
            TempData["PriceError"] = "Could not add the price. Check the values and try again.";
        }
        return RedirectToAction("Edit", "Categories", new { id = categoryPrice.CategoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryPrice categoryPrice)
    {
        if (ModelState.IsValid)
        {
            _db.CategoryPrices.Update(categoryPrice);
            await _db.SaveChangesAsync();
        }
        else
        {
            TempData["PriceError"] = "Could not update the price. Check the values and try again.";
        }
        return RedirectToAction("Edit", "Categories", new { id = categoryPrice.CategoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int categoryId)
    {
        var price = await _db.CategoryPrices.FindAsync(id);
        if (price != null)
        {
            _db.CategoryPrices.Remove(price);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Edit", "Categories", new { id = categoryId });
    }
}
