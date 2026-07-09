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

        // Starter site types (grouped by hookup level). Names carry the amenity summary.
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "RV Site - Full Hookup (Water/Electric/Sewer)" },
            new Category { Id = 2, Name = "RV Site - Water & Electric" },
            new Category { Id = 3, Name = "RV Site - Electric Only" },
            new Category { Id = 4, Name = "Tent Site (No Hookups)" },
            new Category { Id = 5, Name = "Dry Storage" },
            new Category { Id = 6, Name = "Rental Trailer" }
        );

        // Placeholder current prices (EndDate = null). Scott should confirm real rates.
        modelBuilder.Entity<CategoryPrice>().HasData(
            new CategoryPrice { Id = 1, CategoryId = 1, StartDate = new DateTime(2026, 1, 1), EndDate = null, Price = 50m },
            new CategoryPrice { Id = 2, CategoryId = 2, StartDate = new DateTime(2026, 1, 1), EndDate = null, Price = 40m },
            new CategoryPrice { Id = 3, CategoryId = 3, StartDate = new DateTime(2026, 1, 1), EndDate = null, Price = 30m },
            new CategoryPrice { Id = 4, CategoryId = 4, StartDate = new DateTime(2026, 1, 1), EndDate = null, Price = 20m },
            new CategoryPrice { Id = 5, CategoryId = 5, StartDate = new DateTime(2026, 1, 1), EndDate = null, Price = 15m },
            new CategoryPrice { Id = 6, CategoryId = 6, StartDate = new DateTime(2026, 1, 1), EndDate = null, Price = 75m }
        );
    }

    public DbSet<SitePhoto> SitePhotos {get; set;}
    public DbSet<Fee> Fees { get; set; }

    // Adding DbSet for Reservation and ReservationFee ---- Also viable suspect for breaking the code, but I think it is correct.
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationFee> ReservationFees { get; set; }

}