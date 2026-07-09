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

    public DbSet<CategoryPrice> CategoryPrices { get; set; }

    public DbSet<Site> Sites {get; set;}


    public DbSet<Employee> Employees { get; set; }

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
}