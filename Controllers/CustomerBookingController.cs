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

            if (checkIn >= checkOut)
            {
                ModelState.AddModelError("", "Check-out date must be later than check-in.");
                return View(model);
            }

            var availableSites = await _reservationService.FindAvailableSitesAsync(checkIn, checkOut, categoryId, length);
            ViewBag.Results = availableSites;
            return View(model);
        }

        // ========================================================
        // SECURE ACTIONS: RESTRICTED TO LOGGED-IN CUSTOMERS ONLY
        // ========================================================

        [HttpPost]
        public async Task<IActionResult> Reserve(int siteId, DateTime start, DateTime end, int rvLength)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "CustomerAccount");
            }

            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int userId)) return Challenge();

            var site = await context.Sites.FindAsync(siteId);
            if (site == null) return NotFound();

            int totalNights = Math.Max(1, (end - start).Days);
            var newReservation = new Reservation
            {
                UserId = userId,
                SiteId = siteId,
                StartDate = start,
                FinishDate = end,
                RvLength = rvLength,
                DailyRate = 35.00m,
                TotalCost = totalNights * 35.00m,
                ReservationStatus = "Pending Payment"
            };

            context.Reservations.Add(newReservation);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Success! Site '{site.Name}' has been temporarily reserved.";
            return RedirectToAction("Search");
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
