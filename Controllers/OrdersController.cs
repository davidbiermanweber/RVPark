using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

public class OrdersController : Controller
{
    private readonly AppDbContext _db;

    private readonly IConfiguration _config;

    public OrdersController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Site)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    [HttpPost]
public async Task<IActionResult> Cancel(int id)
{
    var order = await _db.Orders.FindAsync(id);
    if (order == null) return RedirectToAction(nameof(Index));

    var payment = await _db.OrderPayments
        .FirstOrDefaultAsync(p => p.OrderId == id);

    if (payment != null)
    {
        StripeConfiguration.ApiKey = _config["stripe:secret_key"];
        var refundService = new RefundService();
        await refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = payment.PaymentToken
        });
        payment.Status = "refunded";
    }

    order.Status = "Cancelled";
    await _db.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}

}
