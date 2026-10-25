using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Tests;

/// <summary>Unit tests for <see cref="FlightService"/> (I1: view expected bags for a departure flight).</summary>
/// <remarks>
/// Uses an in-memory SQLite database so the real EF Core queries (ordering, Include, AsNoTracking)
/// are exercised without needing the production database.
/// </remarks>

public class FlightServiceTests
{
    // ---------- GetFlightsAsync ----------

    [Fact]
    public async Task GetFlights_ReturnsAllFlights()
    {
        await using var db = CreateContext();
        var flights = await new FlightService(db).GetFlightsAsync();
        Assert.Equal(4, flights.Count);
    }

// FlightId order (101,102,103,104) differs from departure order, so a pass proves
// the service sorts by ScheduledDepartureUtc and not by key.
    [Fact]
    public async Task GetFlights_IsOrderedByScheduledDeparture_NotByFlightId()
    {
        await using var db = CreateContext();
        var flights = await new FlightService(db).GetFlightsAsync();
        // Seeded so that FlightId order (101,102,103,104) differs from departure order.
        Assert.Equal(new[] { 103, 101, 104, 102 }, flights.Select(f => f.FlightId).ToArray());
    }

    [Fact]
    public async Task GetFlights_EmptyDatabase_ReturnsEmptyList()
    {
        await using var db = CreateEmptyContext();
        var flights = await new FlightService(db).GetFlightsAsync();
        Assert.NotNull(flights);
        Assert.Empty(flights);
    }

    [Fact]
    public async Task GetFlights_MapsFlightDetails()
    {
        await using var db = CreateContext();
        var flight = (await new FlightService(db).GetFlightsAsync()).Single(f => f.FlightId == 101);
        Assert.Equal("DEMO101", flight.FlightNumber);
        Assert.Equal("BNE", flight.DepartureAirport);
        Assert.Equal("SYD", flight.ArrivalAirport);
        Assert.Equal("DEMO-AIR-101", flight.AircraftId);
        Assert.Equal(LoadingStatus.NotStarted, flight.LoadingStatus);
    }

// The flight list page only needs flight details, so bags should only load via GetManifestAsync.
    [Fact]
    public async Task GetFlights_DoesNotLoadBags()
    {
        await using var db = CreateContext();
        var flights = await new FlightService(db).GetFlightsAsync();
        Assert.All(flights, f => Assert.Empty(f.Bags));
    }

    [Fact]
    public async Task GetFlights_ResultsAreNotTrackedByTheContext()
    {
        await using var db = CreateContext();
        db.ChangeTracker.Clear();
        await new FlightService(db).GetFlightsAsync();
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetFlights_ReflectsStoredLoadingStatus()
    {
        await using var db = CreateContext();
        var flights = await new FlightService(db).GetFlightsAsync();
        Assert.Equal(LoadingStatus.InProgress, flights.Single(f => f.FlightId == 102).LoadingStatus);
        Assert.Equal(LoadingStatus.Completed, flights.Single(f => f.FlightId == 104).LoadingStatus);
    }

    // ---------- GetManifestAsync ----------

    [Fact]
    public async Task GetManifest_ValidFlight_ReturnsFlightWithItsBags()
    {
        await using var db = CreateContext();
        var flight = await new FlightService(db).GetManifestAsync(101);
        Assert.NotNull(flight);
        Assert.Equal(101, flight!.FlightId);
        Assert.Equal(3, flight.Bags.Count);
    }

// Bags are seeded as 003, 001, 002, so the order only matches if the service sorts by Tag.
    [Fact]
    public async Task GetManifest_BagsAreOrderedByTag()
    {
        await using var db = CreateContext();
        var flight = await new FlightService(db).GetManifestAsync(101);
        // Seeded out of order (003, 001, 002).
        Assert.Equal(new[] { "DEMO-101-001", "DEMO-101-002", "DEMO-101-003" },
                     flight!.Bags.Select(b => b.Tag).ToArray());
    }

    [Fact]
    public async Task GetManifest_OnlyContainsBagsForThatFlight()
    {
        await using var db = CreateContext();
        var flight = await new FlightService(db).GetManifestAsync(101);
        Assert.All(flight!.Bags, b => Assert.Equal(101, b.FlightId));
        Assert.DoesNotContain(flight.Bags, b => b.Tag == "DEMO-102-001");
    }

    [Fact]
    public async Task GetManifest_ReturnsBagDetailsNeededByTheManifestPage()
    {
        await using var db = CreateContext();
        var bags = (await new FlightService(db).GetManifestAsync(101))!.Bags;

        var standard = bags.Single(b => b.Tag == "DEMO-101-001");
        Assert.Equal(HandlingTypes.Standard, standard.HandlingType);
        Assert.Equal(BagOutcome.Pending, standard.Outcome);
        Assert.False(standard.IsSpecial);

        var fragile = bags.Single(b => b.Tag == "DEMO-101-003");
        Assert.Equal("Fragile", fragile.HandlingType);
        Assert.Equal("Handle with care. Keep upright.", fragile.HandlingInstruction);
        Assert.True(fragile.IsSpecial);
    }

    [Fact]
    public async Task GetManifest_ShowsRecordedOutcomes()
    {
        await using var db = CreateContext();
        var bags = (await new FlightService(db).GetManifestAsync(101))!.Bags;
        Assert.Equal(BagOutcome.Loaded, bags.Single(b => b.Tag == "DEMO-101-002").Outcome);
    }

    [Fact]
    public async Task GetManifest_FlightWithNoBags_ReturnsFlightWithEmptyBagList()
    {
        await using var db = CreateContext();
        var flight = await new FlightService(db).GetManifestAsync(103);
        Assert.NotNull(flight);
        Assert.Empty(flight!.Bags);
    }

    [Theory]
    [InlineData(999)]   // not in the data
    [InlineData(100)]   // just below the first seeded id
    [InlineData(105)]   // just above the last seeded id
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetManifest_UnknownFlight_ReturnsNull(int flightId)
    {
        await using var db = CreateContext();
        Assert.Null(await new FlightService(db).GetManifestAsync(flightId));
    }

    [Fact]
    public async Task GetManifest_EmptyDatabase_ReturnsNull()
    {
        await using var db = CreateEmptyContext();
        Assert.Null(await new FlightService(db).GetManifestAsync(101));
    }

    [Fact]
    public async Task GetManifest_ResultIsNotTrackedByTheContext()
    {
        await using var db = CreateContext();
        db.ChangeTracker.Clear();
        await new FlightService(db).GetManifestAsync(101);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetManifest_DoesNotChangeStoredData()
    {
        await using var db = CreateContext();
        await new FlightService(db).GetManifestAsync(101);
        db.ChangeTracker.Clear();
        Assert.Equal(BagOutcome.Pending, (await db.Bags.FindAsync(1))!.Outcome);
        Assert.Equal(LoadingStatus.NotStarted, (await db.Flights.FindAsync(101))!.LoadingStatus);
    }

    // ---------- Helpers ----------

    private static AeroLinkDbContext CreateEmptyContext()
    {
        var options = new DbContextOptionsBuilder<AeroLinkDbContext>().UseSqlite("Data Source=:memory:").Options;
        var db = new AeroLinkDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    private static AeroLinkDbContext CreateContext()
    {
        var db = CreateEmptyContext();
        var day = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
        db.Flights.AddRange(
            // FlightId order is deliberately different from departure-time order.
            new Flight { FlightId = 101, FlightLabel = "A", FlightNumber = "DEMO101", DepartureAirport = "BNE", ArrivalAirport = "SYD", AircraftId = "DEMO-AIR-101", ScheduledDepartureUtc = day.AddHours(4), LoadingStatus = LoadingStatus.NotStarted },
            new Flight { FlightId = 102, FlightLabel = "B", FlightNumber = "DEMO102", DepartureAirport = "BNE", ArrivalAirport = "MEL", AircraftId = "DEMO-AIR-102", ScheduledDepartureUtc = day.AddHours(6), LoadingStatus = LoadingStatus.InProgress },
            new Flight { FlightId = 103, FlightLabel = "C", FlightNumber = "DEMO103", DepartureAirport = "BNE", ArrivalAirport = "CBR", AircraftId = "DEMO-AIR-103", ScheduledDepartureUtc = day.AddHours(3), LoadingStatus = LoadingStatus.NotStarted },
            new Flight { FlightId = 104, FlightLabel = "D", FlightNumber = "DEMO104", DepartureAirport = "BNE", ArrivalAirport = "CNS", AircraftId = "DEMO-AIR-104", ScheduledDepartureUtc = day.AddHours(5), LoadingStatus = LoadingStatus.Completed });
        db.Bags.AddRange(
            // Flight 101 bags inserted out of tag order on purpose.
            new Bag { BagId = 3, FlightId = 101, Tag = "DEMO-101-003", HandlingType = "Fragile", HandlingInstruction = "Handle with care. Keep upright." },
            new Bag { BagId = 1, FlightId = 101, Tag = "DEMO-101-001", HandlingType = "Standard", HandlingInstruction = "Standard handling." },
            new Bag { BagId = 2, FlightId = 101, Tag = "DEMO-101-002", HandlingType = "Standard", HandlingInstruction = "Standard handling.", Outcome = BagOutcome.Loaded },
            new Bag { BagId = 4, FlightId = 102, Tag = "DEMO-102-001" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }
}