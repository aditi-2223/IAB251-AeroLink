using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using AeroLink.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AeroLink.Web.Services;

/// <summary>
/// Signs employees in by checking them against the HR system's REST API (T2). The HR API has no login endpoint, so this
/// class fetches the employee by ID and compares the email and job title itself.
/// </summary>
/// <remarks>
/// Invariant: the HR system is only ever read (HTTP GET). Decisions recorded for the team board: email matching ignores
/// letter case; an Employee ID with leading zeros (for example <c>0100101</c>) is accepted and treated as 100101; unknown ID and
/// wrong email share one message so the form never reveals which accounts exist. This is a simulated login, not production security.
/// </remarks>
public class HrAuthService : IHrAuthService
{
    /// <summary>The longest email address that can be valid (RFC 5321).</summary>
    private const int MaxEmailLength = 254;

    private const string EmailRequiredMessage = "Email is required.";
    private const string EmailFormatMessage = "Enter a valid email address, for example name@company.com.";
    private const string EmployeeIdRequiredMessage = "Employee ID is required.";
    private const string EmployeeIdFormatMessage = "Employee ID must be a whole number from 1 to 2147483647.";

    private readonly HttpClient _httpClient;
    private readonly ILogger<HrAuthService> _logger;

    /// <summary>Creates the service.</summary>
    /// <param name="httpClient">A client whose <see cref="HttpClient.BaseAddress"/> is the HR system, for example <c>https://localhost:5001/</c>.</param>
    /// <param name="logger">Optional logger for HR connection problems. Employee details are never logged.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <c>null</c>.</exception>
    public HrAuthService(HttpClient httpClient, ILogger<HrAuthService>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullLogger<HrAuthService>.Instance;
    }

    /// <inheritdoc />
    public async Task<HrAuthResult> AuthenticateAsync(string? email, string? employeeId, CancellationToken cancellationToken = default)
    {
        var cleanEmail = email?.Trim() ?? string.Empty;
        var cleanId = employeeId?.Trim() ?? string.Empty;

        var emailError = ValidateEmail(cleanEmail);
        var idError = ValidateEmployeeId(cleanId, out var id);
        if (emailError is not null || idError is not null)
            return HrAuthResult.ValidationFailed(emailError, idError);

        var lookup = await FetchEmployeeAsync(id, cancellationToken);
        if (lookup.Failure is not null)
            return lookup.Failure;

        var employee = lookup.Employee!;
        if (!string.Equals(employee.Email?.Trim(), cleanEmail, StringComparison.OrdinalIgnoreCase))
            return HrAuthResult.Failed(HrAuthOutcome.InvalidCredentials, HrAuthResult.InvalidCredentialsMessage);

        if (!HrRoles.IsAllowed(employee.JobTitle))
            return HrAuthResult.Failed(HrAuthOutcome.NotAuthorised, HrAuthResult.NotAuthorisedMessage);

        return HrAuthResult.Succeeded(employee);
    }

    /// <summary>Checks an already-trimmed email.</summary>
    /// <returns>An error message, or <c>null</c> if the email is acceptable.</returns>
    private static string? ValidateEmail(string email)
    {
        if (email.Length == 0)
            return EmailRequiredMessage;

        // MailAddress alone also accepts "Name <a@b.com>" and "a@b", so require the parsed address to equal the input
        // and the host to contain a dot.
        var isValid = email.Length <= MaxEmailLength
            && MailAddress.TryCreate(email, out var parsed)
            && string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase)
            && parsed.Host.Contains('.');

        return isValid ? null : EmailFormatMessage;
    }

    /// <summary>Checks an already-trimmed Employee ID and converts it to a number.</summary>
    /// <param name="employeeId">The trimmed text.</param>
    /// <param name="id">The parsed ID when the text is valid, otherwise 0.</param>
    /// <returns>An error message, or <c>null</c> if the ID is acceptable.</returns>
    private static string? ValidateEmployeeId(string employeeId, out int id)
    {
        id = 0;
        if (employeeId.Length == 0)
            return EmployeeIdRequiredMessage;

        // NumberStyles.None allows digits only: no sign, decimal point, spaces or thousands separators.
        var isValid = int.TryParse(employeeId, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
        return isValid ? null : EmployeeIdFormatMessage;
    }

    /// <summary>Calls <c>GET api/employees/{id}</c> and turns every possible problem into a result instead of an exception.</summary>
    /// <param name="id">A valid, positive Employee ID.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The employee on success; otherwise the failure result to return to the caller.</returns>
    /// <exception cref="OperationCanceledException">The caller cancelled the request.</exception>
    private async Task<(HrEmployeeDto? Employee, HrAuthResult? Failure)> FetchEmployeeAsync(int id, CancellationToken cancellationToken)
    {
        var unavailable = HrAuthResult.Failed(HrAuthOutcome.ServiceUnavailable, HrAuthResult.ServiceUnavailableMessage);

        try
        {
            using var response = await _httpClient.GetAsync($"api/employees/{id}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (null, HrAuthResult.Failed(HrAuthOutcome.InvalidCredentials, HrAuthResult.InvalidCredentialsMessage));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("HR API returned unexpected status {StatusCode}.", (int)response.StatusCode);
                return (null, unavailable);
            }

            var employee = await response.Content.ReadFromJsonAsync<HrEmployeeDto>(cancellationToken: cancellationToken);

            // Never trust a reply about a different employee than the one requested.
            if (employee is null || employee.EmployeeId != id)
            {
                _logger.LogWarning("HR API returned an empty or mismatched employee record.");
                return (null, unavailable);
            }

            return (employee, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("The HR API did not respond in time.");
            return (null, unavailable);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            _logger.LogWarning(ex, "The HR API could not be used.");
            return (null, unavailable);
        }
    }
}