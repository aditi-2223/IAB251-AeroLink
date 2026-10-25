namespace AeroLink.Web.Models;

/// <summary>Defines the HR job titles authorised to use AeroLink baggage operations.</summary>
/// <remarks>Invariant: these values must match the HR API's <c>jobTitle</c> values exactly apart from casing.</remarks>
public static class BaggageRoles
{
    /// <summary>Gets the HR job title for a baggage handler.</summary>
    public const string BaggageHandler = "Baggage Handler";

    /// <summary>Gets the HR job title for a baggage supervisor.</summary>
    public const string BaggageSupervisor = "BG_Supervisor";
}
