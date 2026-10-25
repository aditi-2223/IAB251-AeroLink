namespace AeroLink.Web.Models;


/// <summary>
/// Immutable description of how a bag must be handled, passed from the service layer to the pages.
/// </summary>
/// <param name="IsSpecial">Whether the bag needs special handling.</param>
/// <param name="Category">The handling category, for example <c>Fragile</c>.</param>
/// <param name="Instruction">The handling instruction to show the employee; never blank.</param>
/// <remarks>
/// Pre-condition: <paramref name="Category"/> and <paramref name="Instruction"/> are non-null.
/// Post-condition: instances never change after creation.
/// </remarks>
public sealed record SpecialHandlingInfo(bool IsSpecial, string Category, string Instruction)
{
    /// <summary>
    /// Gets whether the employee must acknowledge the instruction before the bag can be confirmed as loaded.
    /// </summary>
    /// <remarks>Derived from <see cref="IsSpecial"/> so the two values can never disagree.</remarks>
    public bool RequiresAcknowledgement => IsSpecial;
}