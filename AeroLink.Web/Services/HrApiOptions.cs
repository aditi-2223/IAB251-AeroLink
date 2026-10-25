namespace AeroLink.Web.Services;

/// <summary>Settings for the connection to the HR system's REST API, read from the <c>HrApi</c> section of appsettings.json (T2).</summary>
public sealed class HrApiOptions
{
    /// <summary>The name of the configuration section these settings are read from.</summary>
    public const string SectionName = "HrApi";

    /// <summary>Gets or sets the address of the HR system, for example <c>https://localhost:5001/</c>.</summary>
    public string BaseUrl { get; set; } = "https://localhost:5001/";

    /// <summary>Gets or sets how many seconds to wait for the HR system before giving up.</summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>Builds the base address used for every HR request.</summary>
    /// <returns>An absolute URI that always ends with a slash, so relative paths such as <c>api/employees/1</c> resolve correctly.</returns>
    /// <exception cref="InvalidOperationException"><see cref="BaseUrl"/> is blank or is not an absolute http or https URL.</exception>
    public Uri GetBaseUri()
    {
        var isValid = Uri.TryCreate(BaseUrl?.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

        if (!isValid)
            throw new InvalidOperationException($"HrApi:BaseUrl '{BaseUrl}' must be an absolute http or https URL.");
        return uri!;
    }

    /// <summary>Gets how long to wait for the HR system.</summary>
    /// <returns>The timeout, using 5 seconds when <see cref="TimeoutSeconds"/> is zero or negative.</returns>
    public TimeSpan GetTimeout() => TimeSpan.FromSeconds(TimeoutSeconds > 0 ? TimeoutSeconds : 5);
}
