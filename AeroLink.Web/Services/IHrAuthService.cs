using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Validates an employee's supplied credentials and baggage role with the external HR API.</summary>
/// <remarks>Pre-condition: none; input is validated by this service. Post-condition: the service never creates an authentication cookie. Exceptional HTTP and JSON conditions are returned as <see cref="HrAuthOutcome.ServiceUnavailable"/>.</remarks>
public interface IHrAuthService
{
    /// <summary>Authenticates an employee email and employee ID against the HR system.</summary>
    /// <param name="email">The email entered by the employee; surrounding whitespace is ignored.</param>
    /// <param name="employeeId">The employee ID entered by the employee; surrounding whitespace is ignored.</param>
    /// <param name="cancellationToken">Token used to cancel the remote HR request.</param>
    /// <returns>A result containing validation errors, a safe failure outcome, or the authorised employee.</returns>
    /// <exception cref="OperationCanceledException">Thrown only when the supplied <paramref name="cancellationToken"/> is cancelled before validation completes.</exception>
    /// <remarks>Pre-condition: none. Post-condition: no remote request is made when local validation fails. Invariant: unknown IDs and mismatched emails receive the same message.</remarks>
    Task<HrAuthResult> AuthenticateAsync(string? email, string? employeeId, CancellationToken cancellationToken = default);
}
