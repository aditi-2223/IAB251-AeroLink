using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Data;

/// <summary>
/// Database context for flights, bags and baggage exceptions.
/// </summary>
public class AeroLinkDbContext : DbContext
{
    /// <summary>Creates the context with the supplied options.</summary>
    /// <param name="options">Provider and connection settings, supplied by dependency injection.</param>
    public AeroLinkDbContext(DbContextOptions<AeroLinkDbContext> options) : base(options) { }

    /// <summary>Departure flights.</summary>
    public DbSet<Flight> Flights => Set<Flight>();

    /// <summary>Bags on the flight manifests.</summary>
    public DbSet<Bag> Bags => Set<Bag>();

    /// <summary>Reported baggage problems and their decisions.</summary>
    public DbSet<BagException> Exceptions => Set<BagException>();

    /// <summary>Configures enum storage and constraints.</summary>
    /// <param name="modelBuilder">The builder used to configure the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // BagException's key is "ExceptionId", which doesn't match EF's naming convention
        // for the class name, so it must be declared explicitly.
        modelBuilder.Entity<BagException>().HasKey(e => e.ExceptionId);

        // Store enums as readable text ("Pending") instead of numbers.
        modelBuilder.Entity<Flight>().Property(f => f.LoadingStatus).HasConversion<string>();
        modelBuilder.Entity<Bag>().Property(b => b.Outcome).HasConversion<string>();
        modelBuilder.Entity<BagException>().Property(e => e.Status).HasConversion<string>();

        // A bag tag identifies exactly one bag across all flights.
        modelBuilder.Entity<Bag>().HasIndex(b => b.Tag).IsUnique();
    }
}