using Microsoft.EntityFrameworkCore;
using RvParkApp.Models;

public class AppDbContext : DbContext
{
    // temporary supressed override
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Suppresses the pending model changes exception on runtime execution
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }


    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Site> Sites {get; set;}

    public DbSet<Employee> Employees { get; set; }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderPayment> OrderPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 💡 THE FIX: Change "Reservation" to "Reservations" (with an "s") to match your active Azure table!
        modelBuilder.Entity<Reservation>().ToTable("Reservations");

        // Keep all your remaining plural mappings exactly the same...
        modelBuilder.Entity<Site>().ToTable("Sites");
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Fee>().ToTable("Fees");
        modelBuilder.Entity<SitePhoto>().ToTable("SitePhotos");
        modelBuilder.Entity<ReservationFee>().ToTable("ReservationFee");
        modelBuilder.Entity<SiteBlock>().ToTable("SiteBlocks");
        modelBuilder.Entity<ParkPolicy>().ToTable("ParkPolicies");

        // Don't cascade-delete reservations when a site is removed; keep history intact.
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Site)
            .WithMany()
            .HasForeignKey(r => r.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backfill existing sites to active and default new rows to active, so adding
        // the column doesn't silently make every current site unbookable.
        modelBuilder.Entity<Site>()
            .Property(s => s.IsActive)
            .HasDefaultValue(true);

        // Money columns: be explicit so SQL Server doesn't default to decimal(18,2)
        // with a truncation warning.
        modelBuilder.Entity<ParkPolicy>()
            .Property(p => p.CancellationFee)
            .HasPrecision(18, 2);

        // Seed the single ParkPolicy row with the requirement defaults (A3/A4):
        // 6-month booking window, Apr–Oct peak w/ 14-day max stay, Oct 15–Apr 1
        // long-term window, 14-day away rule, $10 standard cancellation fee with a
        // 3-day threshold, late/holiday cancellations charge one night.
        modelBuilder.Entity<ParkPolicy>().HasData(
            new ParkPolicy
            {
                Id = 1,
                BookingWindowMonths = 6,
                PeakStartMonth = 4,
                PeakEndMonth = 10,
                PeakMaxStayDays = 14,
                LongTermStartMonth = 10,
                LongTermStartDay = 15,
                LongTermEndMonth = 4,
                LongTermEndDay = 1,
                AwayBeforeReturnDays = 14,
                CancellationFee = 10.00m,
                CancellationThresholdDays = 3,
                LateCancelChargesOneNight = true
            }
        );

        // Seeding your Admin user remains completely safe and untouched below...
        modelBuilder.Entity<Employee>().HasData(
            new Employee
            {
                Id = 1,
                Name = "System Admin",
                EmployeeId = "000001",
                Username = "admin",
                Password = "password",
                AccessLevel = 3
            }
        );
    }

    public DbSet<SitePhoto> SitePhotos {get; set;}
    public DbSet<Fee> Fees { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationFee> ReservationFees { get; set; }
    public DbSet<CategoryPrice> CategoryPrices { get; set; }
    public DbSet<SiteBlock> SiteBlocks { get; set; }
    public DbSet<ParkPolicy> ParkPolicies { get; set; }
}