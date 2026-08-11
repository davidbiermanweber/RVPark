using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RvParkApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Abe's addition for customer reservation service - fully compatible with SQL Server!
builder.Services.AddScoped<CustomerReservationService>();

builder.Services.AddScoped<EmailService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

// Password hashing (NFR-3) and dev email delivery for account verification (G1).
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();

// Restored to feed dependencies into SitesController so the management views load normally
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

//  Configures Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Staff login — the default for admin areas

        // Signed in but lacking the role (e.g. employee hitting an [AdminOnly] page).
        // Stated explicitly: the framework default points at a route that doesn't exist,
        // which surfaced as a bare 404 instead of a denial message.
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Customers and staff sign in through different pages, but cookie auth only
        // supports one LoginPath. Pick the right one from the area the request was
        // headed for, so a guest booking a site lands on the customer sign-in page
        // instead of the employee one.
        options.Events.OnRedirectToLogin = context =>
        {
            var target = context.Request.Path.Value ?? string.Empty;

            bool isCustomerArea =
                target.StartsWith("/CustomerBooking", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("/CustomerAccount", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("/Payment", StringComparison.OrdinalIgnoreCase);

            var loginPath = isCustomerArea ? "/CustomerAccount/Login" : "/Account/Login";

            // Only round-trip a GET. Sending someone back to a POST-only action after
            // signing in would just 405.
            if (HttpMethods.IsGet(context.Request.Method))
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                loginPath += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            context.Response.Redirect(loginPath);
            return Task.CompletedTask;
        };
    });


var app = builder.Build();

Stripe.StripeConfiguration.ApiKey = builder.Configuration["stripe:secret_key"];

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // Restored back to standard migrations tracking rule
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
