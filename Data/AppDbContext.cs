using Microsoft.EntityFrameworkCore;
using RvParkApp.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Site> Sites {get; set;}


    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seeding your Admin user
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
}