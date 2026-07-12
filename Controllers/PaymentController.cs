using Microsoft.AspNetCore.Mvc;
using Stripe;

public class PaymentController : Controller
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public PaymentController(IConfiguration config, AppDbContext db)
    {
        _config = config;
        _db = db;
    }

    public IActionResult Index()
    {
        ViewBag.PublishableKey = _config["stripe:publishable_key"];
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentRequest request)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(request.Amount * 100),
            Currency = "usd",
            PaymentMethodTypes = new List<string> {"card"}
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);
        return Json(new { clientSecret = intent.ClientSecret });
    }

    public IActionResult Success() => View();
    public IActionResult Cancel() => View();

    [HttpPost]
public async Task<IActionResult> SaveOrder([FromBody] SaveOrderRequest request)
{
    var order = new Order
    {
        CustomerId = request.CustomerId,
        SiteId = request.SiteId,
        CheckIn = request.CheckIn,
        CheckOut = request.CheckOut,
        Notes = request.Notes,
        Amount = request.Amount,
        Status = "Paid"
    };
    _db.Orders.Add(order);
    await _db.SaveChangesAsync();

    _db.OrderPayments.Add(new OrderPayment
    {
        OrderId = order.Id,
        PaymentToken = request.PaymentToken,
        Amount = request.Amount,
        Status = "succeeded"
    });
    await _db.SaveChangesAsync();

    return Json(new { success = true });
}

}

public class PaymentRequest
{
    public decimal Amount { get; set; }
}

public class SaveOrderRequest
{
    public int CustomerId { get; set; }
    public int SiteId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentToken { get; set; } = string.Empty;
}
