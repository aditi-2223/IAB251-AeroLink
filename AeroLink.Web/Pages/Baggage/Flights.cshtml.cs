using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>Lets a Baggage Handler choose a departure flight to work on.</summary>
public class FlightsModel : PageModel
{
    private readonly IFlightService _flights;

    /// <summary>Creates the page with the flight service.</summary>
    /// <param name="flights">Service used to list flights.</param>
    public FlightsModel(IFlightService flights) => _flights = flights;

    /// <summary>All departure flights, ordered by scheduled departure.</summary>
    public IReadOnlyList<Flight> Flights { get; private set; } = Array.Empty<Flight>();

    /// <summary>Loads the flight list when the page is requested.</summary>
    public async Task OnGetAsync() => Flights = await _flights.GetFlightsAsync();
}