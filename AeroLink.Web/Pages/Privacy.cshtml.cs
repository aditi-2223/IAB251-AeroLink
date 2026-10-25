using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>Provides the AeroLink privacy page.</summary>
public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> _logger;

    /// <summary>Creates the privacy-page model.</summary>
    /// <param name="logger">Logger supplied by dependency injection.</param>
    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        _logger = logger;
    }

    /// <summary>Handles requests for the privacy page.</summary>
    public void OnGet()
    {
    }
}

