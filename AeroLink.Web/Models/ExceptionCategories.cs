namespace AeroLink.Web.Models;

/// <summary>
/// Names of the problem categories a handler can choose when reporting a bag exception (I4).
/// </summary>
/// <remarks>
/// Values must match the <c>Category</c> column of the supplied manifest's Exceptions sheet exactly
/// (comparison is case-insensitive), so reported and seeded exceptions use the same vocabulary.
/// </remarks>
public static class ExceptionCategories
{
    /// <summary>The bag is on the manifest but cannot be found in the loading area.</summary>
    public const string MissingBag = "MissingBag";

    /// <summary>The bag was found but is visibly damaged.</summary>
    public const string DamagedBag = "DamagedBag";

    /// <summary>Gets every category a handler may choose, in the order shown on the form.</summary>
    public static IReadOnlyList<string> All { get; } = new[] { MissingBag, DamagedBag };

    /// <summary>Returns the canonical spelling of a category, ignoring case and surrounding whitespace.</summary>
    /// <param name="category">The category text to look up; may be <c>null</c>.</param>
    /// <returns>The matching constant (for example <see cref="MissingBag"/>), or <c>null</c> when it is not a known category.</returns>
    /// <remarks>Post-condition: no state changes.</remarks>
    public static string? Normalise(string? category) =>
        All.FirstOrDefault(c => string.Equals(c, category?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Converts a category into text suitable for display, for example <c>Missing bag</c>.</summary>
    /// <param name="category">The stored category value.</param>
    /// <returns>The category with spaces inserted before each capital letter, or the original text when it is blank.</returns>
    /// <remarks>Post-condition: no state changes.</remarks>
    public static string DisplayName(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return "";
        var text = System.Text.RegularExpressions.Regex.Replace(category.Trim(), "(?<!^)([A-Z])", " $1");
        return text.Substring(0, 1) + text.Substring(1).ToLowerInvariant();
    }
}