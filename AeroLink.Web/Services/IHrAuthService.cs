using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Checks an employee's email address and Employee ID against the HR system so they can sign in to AeroLink (T2).</summary>
public interface IHrAuthService
{
    /// <summary>
    /// Validates the sign-in form and, if it is valid, asks the HR system whether the employee exists, the email
    /// matches, and the role is Baggage Handler or BG_Supervisor.
    /// </summary>
    /// <param name="email">The email the employee typed. Surrounding spaces are ignored; letter case is ignored when matching.</param>
    /// <param name="employeeId">The Employee ID the employee typed. Surrounding spaces are ignored. It must be a whole number from 1 to 2147483647.</param>
    /// <param name="cancellationToken">Cancels the request, for example when the browser disconnects.</param>
    /// <returns>
    /// A result whose outcome is <see cref="HrAuthOutcome.Success"/> with the verified employee, or the reason sign-in failed
    /// together with a message and any per-field errors for display.
    /// </returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled by the caller.</exception>
    /// <remarks>
    /// Pre-conditions: none. The inputs may be <c>null</c>, blank or malformed, because they come straight from a web form.
    /// Post-conditions: the HR system is called at most once, and only after both fields pass validation. Nothing is stored or
    /// changed in either system, and the employee is not signed in by this method; the caller does that on success.
    /// Exceptional circumstances: if the HR system is down, times out, returns an unexpected status code, or returns JSON that
    /// cannot be read, no exception is thrown and the outcome is <see cref="HrAuthOutcome.ServiceUnavailable"/>. Only a caller
    /// cancellation escapes as an exception.
    /// </remarks>
    Task<HrAuthResult> AuthenticateAsync(string? email, string? employeeId, CancellationToken cancellationToken = default);
}