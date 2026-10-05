using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>Shows the expected baggage manifest for one departure flight.</summary>
public class ManifestModel : PageModel
{
    private readonly IFlightService _flights;

    /// <summary>Creates the page with the flight service.</summary>
    /// <param name="flights">Service used to read the manifest.</param>
    public ManifestModel(IFlightService flights) => _flights = flights;

    /// <summary>The selected flight and its bags, once loaded.</summary>
    public Flight? Flight { get; private set; }

    /// <summary>Loads the manifest for the requested flight.</summary>
    /// <param name="flightId">Flight identifier from the query string.</param>
    /// <returns>The page if found, otherwise a 404 result.</returns>
    public async Task<IActionResult> OnGetAsync(int flightId)
    {
        Flight = await _flights.GetManifestAsync(flightId);
        return Flight is null ? NotFound() : Page();
    }
}