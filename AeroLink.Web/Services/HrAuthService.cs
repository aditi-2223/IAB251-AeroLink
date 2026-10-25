using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Uses the HR employee endpoint to authenticate authorised AeroLink baggage employees.</summary>
/// <remarks>Pre-condition: the injected client has an HR API base address. Post-condition: this service never persists data or creates cookies. HTTP, timeout, and JSON failures are safely mapped to <see cref="HrAuthOutcome.ServiceUnavailable"/>.</remarks>
public sealed class HrAuthService : IHrAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or employee ID.";
    private const string NotAuthorisedMessage = "You do not have permission to access the Baggage module.";
    private const string ServiceUnavailableMessage = "Unable to verify your details right now. Please try again later.";
    private readonly HttpClient _httpClient;

    /// <summary>Creates an HR authentication service using the configured typed HTTP client.</summary>
    /// <param name="httpClient">Client configured with the HR API base URL and timeout.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is <c>null</c>.</exception>
    /// <remarks>Pre-condition: the client base address targets the HR API. Post-condition: the client is retained for subsequent requests.</remarks>
    public HrAuthService(HttpClient httpClient) => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public async Task<HrAuthResult> AuthenticateAsync(string? email, string? employeeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalisedEmail = email?.Trim();
        var normalisedEmployeeId = employeeId?.Trim();
        var emailError = GetEmailError(normalisedEmail);
        var employeeIdError = GetEmployeeIdError(normalisedEmployeeId, out var parsedEmployeeId);

        if (emailError is not null || employeeIdError is not null)
        {
            return new HrAuthResult
            {
                Outcome = HrAuthOutcome.ValidationFailed,
                Message = "Please correct the highlighted fields.",
                EmailError = emailError,
                EmployeeIdError = employeeIdError
            };
        }

        try
        {
            using var response = await _httpClient.GetAsync($"api/employees/{parsedEmployeeId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return Failure(HrAuthOutcome.InvalidCredentials, InvalidCredentialsMessage);

            if (!response.IsSuccessStatusCode)
                return Failure(HrAuthOutcome.ServiceUnavailable, ServiceUnavailableMessage);

            var employee = await response.Content.ReadFromJsonAsync<HrEmployeeDto>(cancellationToken: cancellationToken);
            if (employee is null || !string.Equals(employee.Email?.Trim(), normalisedEmail, StringComparison.OrdinalIgnoreCase))
                return Failure(HrAuthOutcome.InvalidCredentials, InvalidCredentialsMessage);

            if (!IsAllowedRole(employee.JobTitle))
                return Failure(HrAuthOutcome.NotAuthorised, NotAuthorisedMessage);

            return new HrAuthResult { Outcome = HrAuthOutcome.Success, Message = string.Empty, Employee = employee };
        }
        catch (HttpRequestException)
        {
            return Failure(HrAuthOutcome.ServiceUnavailable, ServiceUnavailableMessage);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(HrAuthOutcome.ServiceUnavailable, ServiceUnavailableMessage);
        }
        catch (JsonException)
        {
            return Failure(HrAuthOutcome.ServiceUnavailable, ServiceUnavailableMessage);
        }
    }

    /// <summary>Validates email syntax using MailAddress because it is a concise syntax check, not mailbox verification.</summary>
    private static string? GetEmailError(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email is required";

        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ? null : "Enter a valid email address.";
        }
        catch (FormatException)
        {
            return "Enter a valid email address.";
        }
    }

    /// <summary>Validates and parses the positive whole-number employee ID.</summary>
    private static string? GetEmployeeIdError(string? employeeId, out int parsedEmployeeId)
    {
        parsedEmployeeId = 0;
        if (string.IsNullOrWhiteSpace(employeeId))
            return "Employee ID is required";

        if (!int.TryParse(employeeId, NumberStyles.None, CultureInfo.InvariantCulture, out parsedEmployeeId) || parsedEmployeeId <= 0)
            return "Employee ID must be a positive whole number.";

        return null;
    }

    /// <summary>Determines whether an HR job title grants baggage-module access.</summary>
    private static bool IsAllowedRole(string? role) =>
        string.Equals(role, BaggageRoles.BaggageHandler, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, BaggageRoles.BaggageSupervisor, StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates a safe non-success authentication result.</summary>
    private static HrAuthResult Failure(HrAuthOutcome outcome, string message) => new() { Outcome = outcome, Message = message };
}
