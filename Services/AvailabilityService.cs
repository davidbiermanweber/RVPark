using Microsoft.EntityFrameworkCore;

// Single source of truth for "is this site free?" — shared by the customer-facing
// availability search (G3/G9) and the admin availability grid (Epic 3). A site is
// available for [start, end) when it is active, has no overlapping non-cancelled
// reservation, and has no overlapping maintenance block.
public interface IAvailabilityService
{
    Task<bool> IsSiteAvailableAsync(int siteId, DateTime start, DateTime end, int? ignoreReservationId = null);
    Task<List<Site>> AvailableSitesAsync(DateTime start, DateTime end, int? categoryId = null, int? minRvLength = null);
}

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _db;

    public AvailabilityService(AppDbContext db) => _db = db;

    // Half-open overlap test: two ranges [aStart,aEnd) and [bStart,bEnd) overlap when
    // aStart < bEnd && bStart < aEnd. Checkout day == another guest's checkin day is fine.
    public async Task<bool> IsSiteAvailableAsync(int siteId, DateTime start, DateTime end, int? ignoreReservationId = null)
    {
        var site = await _db.Sites.FindAsync(siteId);
        if (site == null || !site.IsActive) return false;

        bool reserved = await _db.Reservations.AnyAsync(r =>
            r.SiteId == siteId &&
            (ignoreReservationId == null || r.Id != ignoreReservationId) &&
            !r.ReservationStatus.Contains("Cancelled") &&
            r.StartDate < end && start < r.FinishDate);
        if (reserved) return false;

        bool blocked = await _db.SiteBlocks.AnyAsync(b =>
            b.SiteId == siteId &&
            b.StartDate < end && start < b.EndDate);

        return !blocked;
    }

    public async Task<List<Site>> AvailableSitesAsync(DateTime start, DateTime end, int? categoryId = null, int? minRvLength = null)
    {
        var candidates = _db.Sites.Include(s => s.Category).Where(s => s.IsActive);

        if (categoryId.HasValue)
            candidates = candidates.Where(s => s.CategoryId == categoryId.Value);

        // MaxRvLength == 0 means "unspecified" and is not filtered out.
        if (minRvLength.HasValue)
            candidates = candidates.Where(s => s.MaxRvLength == 0 || s.MaxRvLength >= minRvLength.Value);

        var sites = await candidates.ToListAsync();

        var reservedSiteIds = await _db.Reservations
            .Where(r => r.SiteId != null && !r.ReservationStatus.Contains("Cancelled") && r.StartDate < end && start < r.FinishDate)
            .Select(r => r.SiteId!.Value)
            .Distinct()
            .ToListAsync();

        var blockedSiteIds = await _db.SiteBlocks
            .Where(b => b.StartDate < end && start < b.EndDate)
            .Select(b => b.SiteId)
            .Distinct()
            .ToListAsync();

        var unavailable = new HashSet<int>(reservedSiteIds);
        unavailable.UnionWith(blockedSiteIds);

        return sites.Where(s => !unavailable.Contains(s.Id)).ToList();
    }
}
