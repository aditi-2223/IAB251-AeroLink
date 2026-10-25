using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>Provides the signed-in AeroLink home page.</summary>
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    /// <summary>Creates the home-page model.</summary>
    /// <param name="logger">Logger supplied by dependency injection.</param>
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    /// <summary>Handles requests for the home page.</summary>
    public void OnGet()
    {

    }
}
