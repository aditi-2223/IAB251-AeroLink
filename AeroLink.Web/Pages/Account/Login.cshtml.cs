using System.Security.Claims;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Account;

/// <summary>
/// The sign-in page (T2). An employee enters their email and Employee ID; <see cref="IHrAuthService"/> checks them against the
/// HR system, and on success the employee is signed in with the default cookie scheme set up in <c>Program.cs</c>.
/// The page holds no business rules: it passes the form to the service and shows the result.
/// </summary>
public class LoginModel : PageModel
{
    private readonly IHrAuthService _hrAuthService;

    /// <summary>Creates the page.</summary>
    /// <param name="hrAuthService">The service that validates employees against the HR system.</param>
    public LoginModel(IHrAuthService hrAuthService)
    {
        _hrAuthService = hrAuthService;
    }

    /// <summary>Gets or sets the email address typed on the form.</summary>
    [BindProperty]
    public string? Email { get; set; }

    /// <summary>Gets or sets the Employee ID typed on the form. Kept as text so letters and decimals can be reported, not silently dropped.</summary>
    [BindProperty]
    public string? EmployeeId { get; set; }

    /// <summary>Gets or sets the local URL to return to after a successful sign-in.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Gets the page-level error shown above the form when sign-in fails; <c>null</c> when there is nothing to show.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Shows the sign-in form, or skips it when the employee is already signed in.</summary>
    /// <returns>A redirect to the main screen if already signed in; otherwise the form.</returns>
    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();
    }

    /// <summary>Checks the submitted details with the HR system and signs the employee in if they pass.</summary>
    /// <param name="cancellationToken">Cancelled if the browser disconnects.</param>
    /// <returns>
    /// A redirect to <see cref="ReturnUrl"/> (if it is a local URL) or the main screen on success; otherwise the same page
    /// with field errors or <see cref="ErrorMessage"/>. The employee is not signed in on any failure.
    /// </returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _hrAuthService.AuthenticateAsync(Email, EmployeeId, cancellationToken);

        if (!result.IsSuccess)
        {
            ShowFailure(result);
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(result.Employee!));

        return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Index");
    }

    /// <summary>Copies a failed result onto the page so the view can display it.</summary>
    /// <param name="result">The failed result.</param>
    private void ShowFailure(HrAuthResult result)
    {
        if (result.EmailError is not null)
            ModelState.AddModelError(nameof(Email), result.EmailError);
        if (result.EmployeeIdError is not null)
            ModelState.AddModelError(nameof(EmployeeId), result.EmployeeIdError);

        // Field errors already explain a validation failure, so only other failures need the banner.
        if (result.Outcome != HrAuthOutcome.ValidationFailed)
            ErrorMessage = result.Message;
    }

    /// <summary>Builds the signed-in identity stored in the cookie.</summary>
    /// <param name="employee">The verified employee.</param>
    /// <returns>A principal with the employee's ID, full name, email and role (job title) as claims.</returns>
    private static ClaimsPrincipal CreatePrincipal(HrEmployeeDto employee)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
            new(ClaimTypes.Name, employee.FullName),
            new(ClaimTypes.Email, employee.Email),
            new(ClaimTypes.Role, employee.JobTitle)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}