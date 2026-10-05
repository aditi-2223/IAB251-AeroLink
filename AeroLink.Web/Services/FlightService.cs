using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <summary>Reads departure flights and their expected baggage manifests.</summary>
public interface IFlightService
{
    /// <summary>Returns all departure flights, ordered by scheduled departure time.</summary>
    Task<IReadOnlyList<Flight>> GetFlightsAsync();

    /// <summary>Returns a flight with its bags loaded, ordered by tag.</summary>
    /// <param name="flightId">The flight to look up.</param>
    /// <returns>The flight and its bags, or <c>null</c> if no flight with that ID exists.</returns>
    Task<Flight?> GetManifestAsync(int flightId);
}

/// <inheritdoc cref="IFlightService"/>
public class FlightService : IFlightService
{
    private readonly AeroLinkDbContext _db;

    /// <summary>Creates the service with the given database context.</summary>
    /// <param name="db">Database context, supplied by dependency injection.</param>
    public FlightService(AeroLinkDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Flight>> GetFlightsAsync() =>
        await _db.Flights
            .AsNoTracking()
            .OrderBy(f => f.ScheduledDepartureUtc)
            .ToListAsync();

    /// <inheritdoc />
    public async Task<Flight?> GetManifestAsync(int flightId) =>
        await _db.Flights
            .AsNoTracking()
            .Include(f => f.Bags.OrderBy(b => b.Tag))
            .FirstOrDefaultAsync(f => f.FlightId == flightId);
}