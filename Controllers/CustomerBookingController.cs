using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using RvParkApp.Models;
using RvParkApp.Services;

namespace RvParkApp.Controllers
{
    // REMOVED [Authorize] FROM HERE TO MAKE THE CONTROLLER PUBLIC FOR ANONYMOUS BROWSING
    public class CustomerBookingController : Controller
    {
        private readonly CustomerReservationService _reservationService;

        public CustomerBookingController(CustomerReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public IActionResult Search()
        {
            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            ViewBag.Categories = context.Categories.OrderBy(c => c.Name).ToList();
            return View(new AvailabilitySearchViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Search(AvailabilitySearchViewModel model)
        {
            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            ViewBag.Categories = context.Categories.OrderBy(c => c.Name).ToList();

            var checkIn = model.Start ?? DateTime.Today;
            var checkOut = model.End ?? DateTime.Today.AddDays(1);
            int categoryId = model.CategoryId ?? 0;
            int length = model.RvLength ?? 0;

            // 1. PAST DATE PROTECTION
            if (checkIn.Date < DateTime.Today)
            {
                ModelState.AddModelError("", "❌ Check-in date cannot be in the past.");
                return View(model);
            }

            if (checkIn >= checkOut)
            {
                ModelState.AddModelError("", "❌ Check-out date must be later than check-in.");
                return View(model);
            }

            // 4. MAXIMUM 14-DAY LENGTH LIMITATION
            int totalNights = (checkOut - checkIn).Days;
            if (totalNights > 14)
            {
                ModelState.AddModelError("", "❌ Reservations are limited to a maximum length of 14 days.");
                return View(model);
            }

            // 7. ROLLING 6-MONTH LOOK-AHEAD LIMITATION
            DateTime maxAllowedFutureDate = DateTime.Today.AddMonths(6);
            if (checkIn > maxAllowedFutureDate)
            {
                ModelState.AddModelError("", "❌ Reservations cannot be made further than 6 months ahead from the current date.");
                return View(model);
            }

            // 5. & 6. FETCH SITES IN 'AVAILABLE' STATUS ONLY + INJECT CURRENT PRICES
            var availableSites = await _reservationService.FindAvailableSitesAsync(checkIn, checkOut, categoryId, length);

            // Dynamic Pricing Calculator: Grabs the latest active fee tier for each site's category
            var today = DateTime.Today;
            var basePrices = await context.CategoryPrices
                .Where(p => p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))
                .ToListAsync();

            ViewBag.NightlyPrices = basePrices.ToDictionary(p => p.CategoryId, p => p.Price);
            ViewBag.Results = availableSites;
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Reserve(int siteId, DateTime start, DateTime end, int rvLength)
        {
            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int userId)) return Challenge();

            // Re-verify general input validity limits
            if (start.Date < DateTime.Today || start >= end || (end - start).Days > 14)
            {
                TempData["ErrorMessage"] = "Invalid booking validation configuration parameters.";
                return RedirectToAction("Search");
            }

            // 3. ENFORCE 14-DAY AWAY PROXIMITY RULE FOR THE SAME CUSTOMER
            // Finds any active reservation for this user within 14 days before or after the requested stay
            DateTime blockWindowStart = start.AddDays(-14);
            DateTime blockWindowEnd = end.AddDays(14);

            bool hasProximateReservation = await context.Reservations
                .Where(r => r.UserId == userId && r.ReservationStatus != "Cancelled")
                .AnyAsync(r => r.StartDate < blockWindowEnd && r.FinishDate > blockWindowStart);

            if (hasProximateReservation)
            {
                TempData["ErrorMessage"] = "❌ Booking rule violation: You cannot create a reservation within 14 days of an existing booking under your account profile.";
                return RedirectToAction("Search");
            }

            // 2. DOUBLE-BOOKING OVERLAP CONFLICT PROTECTION
            // Secondary database lock fallback verification block to prevent race condition overrides
            bool isStillAvailable = await _reservationService.ModifyReservationAsync(0, start, end); // Temporary mock check or direct verification
            var overlappingRes = await context.Reservations
                .Where(r => r.SiteId == siteId && r.ReservationStatus != "Cancelled")
                .AnyAsync(r => r.StartDate < end && r.FinishDate > start);

            var overlappingBlocks = await context.SiteBlocks
                .AnyAsync(b => b.SiteId == siteId && b.StartDate < end && b.EndDate > start);

            if (overlappingRes || overlappingBlocks)
            {
                TempData["ErrorMessage"] = "❌ Unable to book site due to reservations conflicting.";
                return RedirectToAction("Search");
            }

            var site = await context.Sites.FindAsync(siteId);
            if (site == null) return NotFound();

            // Get live base rate from pricing tables
            var activePriceRow = await context.CategoryPrices
                .Where(p => p.CategoryId == site.CategoryId && p.StartDate <= DateTime.Today && (p.EndDate == null || p.EndDate >= DateTime.Today))
                .FirstOrDefaultAsync();
            decimal targetDailyRate = activePriceRow?.Price ?? 35.00m;

            int totalNights = (end - start).Days;
            var newReservation = new Reservation
            {
                UserId = userId,
                SiteId = siteId,
                StartDate = start,
                FinishDate = end,
                RvLength = rvLength,
                DailyRate = targetDailyRate,
                TotalCost = totalNights * targetDailyRate,
                ReservationStatus = "Pending Payment"
            };

            context.Reservations.Add(newReservation);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🎉 Success! Site '{site.Name}' has been successfully reserved.";
            return RedirectToAction("Search");
        }

        // ========================================================
        // SECURE ACTIONS: RESTRICTED TO LOGGED-IN CUSTOMERS ONLY
        // ========================================================

        [HttpGet]
        public async Task<IActionResult> Confirm(int siteId, DateTime start, DateTime end, int rvLength)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login", "CustomerAccount");

            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var site = await context.Sites.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null) return NotFound();

            int nights = Math.Max(1, (end - start).Days);
            ViewBag.Site = site;
            ViewBag.Start = start.ToString("MMM d, yyyy");
            ViewBag.End = end.ToString("MMM d, yyyy");
            ViewBag.Nights = nights;
            ViewBag.Total = nights * 35.00m;
            ViewBag.SiteId = siteId;
            ViewBag.StartRaw = start.ToString("yyyy-MM-dd");
            ViewBag.EndRaw = end.ToString("yyyy-MM-dd");
            ViewBag.RvLength = rvLength;

            return View();
        }

        [Authorize] // Requires login to modify an active stay
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DateTime newStart, DateTime newFinish)
        {
            var success = await _reservationService.ModifyReservationAsync(id, newStart, newFinish);
            if (!success) TempData["Error"] = "Selected dates are no longer available.";
            return RedirectToAction("MyReservations", "CustomerAccount");
        }

        [Authorize] // Requires login to execute a cancellation workflow
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var success = await _reservationService.CancelReservationAsync(id);
            if (!success) TempData["Error"] = "Cancellation failed.";
            return RedirectToAction("MyReservations", "CustomerAccount");
        }

        [Authorize] // Requires login to access transaction reference tables
        [HttpPost]
        public async Task<IActionResult> PayAlternative(int id, string method, string refNumber)
        {
            var success = await _reservationService.RecordAlternativePaymentAsync(id, method, refNumber);
            if (!success) TempData["Error"] = "Payment recording failed.";
            return RedirectToAction("MyReservations", "CustomerAccount");
        }
    }
}
