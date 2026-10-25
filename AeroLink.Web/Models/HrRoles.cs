namespace AeroLink.Web.Models;

/// <summary>
/// The HR job titles (roles) that may use the Baggage Unloading, Transfer and Loading Management module (T2).
/// The values must match the <c>jobTitle</c> strings in the HR system exactly.
/// </summary>
public static class HrRoles
{
    /// <summary>A baggage handler who unloads, transfers and loads bags.</summary>
    public const string BaggageHandler = "Baggage Handler";

    /// <summary>A baggage supervisor. The HR system spells this role with an underscore.</summary>
    public const string BaggageSupervisor = "BG_Supervisor";

    /// <summary>Checks whether a job title may use the Baggage module.</summary>
    /// <param name="jobTitle">The job title from the HR system; may be <c>null</c> or blank.</param>
    /// <returns>
    /// <c>true</c> if the title is <see cref="BaggageHandler"/> or <see cref="BaggageSupervisor"/>
    /// (ignoring letter case and surrounding spaces); otherwise <c>false</c>.
    /// </returns>
    /// <remarks>Post-condition: never throws, including for <c>null</c>.</remarks>
    public static bool IsAllowed(string? jobTitle)
    {
        var title = jobTitle?.Trim();
        return string.Equals(title, BaggageHandler, StringComparison.OrdinalIgnoreCase)
            || string.Equals(title, BaggageSupervisor, StringComparison.OrdinalIgnoreCase);
    }
}
