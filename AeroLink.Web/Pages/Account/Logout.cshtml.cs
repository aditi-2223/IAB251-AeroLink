using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Account;

/// <summary>
/// Signs the current user out (T3). Has no view of its own; it clears the
/// authentication cookie and redirects to the sign-in page.
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>
    /// Handles a direct visit to /Account/Logout (for example, typing the URL).
    /// Does not sign the user out, because logout must be an explicit POST.
    /// </summary>
    /// <returns>A redirect to the main screen.</returns>
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Removes the authentication cookie, ending the user's session.
    /// </summary>
    /// <returns>A redirect to the sign-in page.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }
}