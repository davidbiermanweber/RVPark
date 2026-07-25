using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RvParkApp.Models;

namespace RvParkApp.Services
{
    public class CustomerReservationService
    {
        private readonly AppDbContext _db;

        public CustomerReservationService(AppDbContext db)
        {
            _db = db;
        }

        // FEATURE 1 & 2: Campsite Matching (Check-in/out, CategoryId, RV Length)
        public async Task<List<Site>> FindAvailableSitesAsync(DateTime checkIn, DateTime checkOut, int categoryId, int rvLength)
        {
            var overlappingReservations = await _db.Reservations
                .Where(r => r.ReservationStatus != "Cancelled" && r.SiteId.HasValue)
                .Where(r => r.StartDate < checkOut && r.FinishDate > checkIn)
                .Select(r => r.SiteId!.Value)
                .ToListAsync();

            var overlappingBlocks = await _db.Set<SiteBlock>()
                .Where(b => b.StartDate < checkOut && b.EndDate > checkIn)
                .Select(b => b.SiteId)
                .ToListAsync();

            return await _db.Sites
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .Where(s => s.CategoryId == categoryId)
                .Where(s => s.MaxRvLength >= rvLength)
                .Where(s => !overlappingReservations.Contains(s.Id) && !overlappingBlocks.Contains(s.Id))
                .ToListAsync();
        }

        // FEATURE 3: Edit Reservation (Fresh Validation & Re-Pricing Calculation)
        public async Task<bool> ModifyReservationAsync(int reservationId, DateTime newStart, DateTime newFinish)
        {
            var reservation = await _db.Reservations.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null || reservation.ReservationStatus == "Cancelled") return false;

            var isConflict = await _db.Reservations
                .Where(r => r.Id != reservationId && r.SiteId == reservation.SiteId && r.ReservationStatus != "Cancelled")
                .AnyAsync(r => r.StartDate < newFinish && r.FinishDate > newStart);

            if (isConflict) return false;

            reservation.StartDate = newStart;
            reservation.FinishDate = newFinish;

            // Automated total cost recalculation based on nightly stay metrics
            int totalNights = (newFinish - newStart).Days;
            reservation.TotalCost = totalNights * reservation.DailyRate;

            await _db.SaveChangesAsync();
            return true;
        }

        // FEATURE 3: Cancel Reservation
        public async Task<bool> CancelReservationAsync(int reservationId)
        {
            var reservation = await _db.Reservations.FindAsync(reservationId);
            if (reservation == null) return false;

            reservation.ReservationStatus = "Cancelled";
            await _db.SaveChangesAsync();
            return true;
        }

        // FEATURE 4: Record Transactions (Alternative Cash/Check Logging)
        public async Task<bool> RecordAlternativePaymentAsync(int reservationId, string paymentMethod, string referenceNumber)
        {
            var reservation = await _db.Reservations.FindAsync(reservationId);
            if (reservation == null) return false;

            reservation.ReservationStatus = $"Paid via {paymentMethod}";
            if (!string.IsNullOrWhiteSpace(referenceNumber))
            {
                reservation.ReservationStatus += $" (Ref: {referenceNumber})";
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
