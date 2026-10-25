using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>Shows the expected baggage manifest for one departure flight.</summary>
public class ManifestModel : PageModel
{
    private readonly IFlightService _flights;
    private readonly ISpecialHandlingService _specialHandling;

    /// <summary>Creates the page with its flight and special-handling services.</summary>
    /// <param name="flights">Service used to read the manifest.</param>
    /// <param name="specialHandling">Service that identifies special bags and supplies their instructions.</param>
    public ManifestModel(IFlightService flights, ISpecialHandlingService specialHandling)
    {
        _flights = flights;
        _specialHandling = specialHandling;
    }

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

    /// <summary>Gets the handling category and instruction to display for a bag.</summary>
    /// <param name="bag">A bag from the displayed manifest.</param>
    /// <returns>The bag's handling information, as decided by the special-handling service.</returns>
    public SpecialHandlingInfo GetHandlingInfo(Bag bag) => _specialHandling.GetInfo(bag);
}