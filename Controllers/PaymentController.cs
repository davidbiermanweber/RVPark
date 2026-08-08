using Microsoft.AspNetCore.Mvc;
using Stripe;
using Microsoft.AspNetCore.Authorization;

[EmployeeOnly]

public class PaymentController : Controller
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    private readonly EmailService _email;

    public PaymentController(IConfiguration config, AppDbContext db, EmailService email)
    {
        _config = config;
        _db = db;
        _email = email;
    }

    public IActionResult Index()
    {
        ViewBag.PublishableKey = _config["stripe:publishable_key"];
        return View();
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    public IActionResult Success() => View();
    public IActionResult Cancel() => View();

    [AllowAnonymous]
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
        Status = "Paid - Credit Card"
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

    if (request.ReservationId.HasValue)
{
    var reservation = await _db.Reservations.FindAsync(request.ReservationId.Value);
    if (reservation != null)
    {
        reservation.ReservationStatus = $"Paid via {request.PaymentMethod}";
        await _db.SaveChangesAsync();
    }
}

    var emailTo = request.CustomerEmail;
    if (string.IsNullOrEmpty(emailTo) && request.CustomerId > 0)
    {
        var customer = await _db.Users.FindAsync(request.CustomerId);
        if (customer != null) emailTo = customer.Email;
    }
    if (!string.IsNullOrEmpty(emailTo))
        await _email.SendOrderConfirmationAsync(emailTo, order.Id, order.Amount);

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
    public string CustomerEmail {get; set;} = string.Empty;
    public int? ReservationId { get; set; }
    public string PaymentMethod { get; set; } = "card";
}
