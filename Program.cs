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
        options.LoginPath = "/Account/Login"; // Redirects here if not logged in
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
