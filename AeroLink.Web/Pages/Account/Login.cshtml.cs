using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Account;

/// <summary>
/// TEMPORARY stub login page (T1). Signs in any non-empty name so the rest of
/// the workflow can be built and demonstrated. To be replaced by the HR-system
/// login (T2), which must keep the route /Account/Login and sign in using the
/// default cookie scheme.
/// </summary>
public class LoginModel : PageModel
{
    /// <summary>The name entered by the user on the sign-in form.</summary>
    [BindProperty]
    public string Username { get; set; } = string.Empty;

    /// <summary>The local URL to return to after a successful sign-in.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Displays the sign-in form.</summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Validates the form and signs the user in with a cookie.
    /// </summary>
    /// <returns>
    /// A redirect to <see cref="ReturnUrl"/> (if local) or the main screen on
    /// success; the same page with an error message if the name is empty.
    /// </returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ModelState.AddModelError(nameof(Username), "Please enter your name.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, Username.Trim()),
            new(ClaimTypes.Role, "Baggage Handler")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }
        return RedirectToPage("/Index");
    }
}