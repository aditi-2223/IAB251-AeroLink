using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Identifies special baggage, supplies its handling instruction, and enforces the rule that special
/// instructions must be acknowledged before a bag is confirmed as loaded.
/// </summary>
public interface ISpecialHandlingService
{
    /// <summary>Describes how a bag must be handled.</summary>
    /// <param name="bag">The bag to describe.</param>
    /// <returns>The handling category and instruction, and whether acknowledgement is required.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="bag"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Pre-condition: <paramref name="bag"/> is not null.
    /// Post-condition: the returned instruction is never blank and the bag is not modified.
    /// </remarks>
    SpecialHandlingInfo GetInfo(Bag bag);

    /// <summary>Decides whether a bag may be confirmed as loaded, given whether the employee acknowledged its instruction.</summary>
    /// <param name="bag">The bag the employee wants to confirm.</param>
    /// <param name="acknowledged">Whether the employee acknowledged the special-handling instruction.</param>
    /// <returns>A decision stating whether loading is allowed, with a message for the employee.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="bag"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Pre-condition: <paramref name="bag"/> is not null.
    /// Post-condition: a special bag is allowed only when <paramref name="acknowledged"/> is <c>true</c>;
    /// a standard bag is always allowed. The bag is not modified.
    /// </remarks>
    LoadingDecision CanConfirmLoading(Bag bag, bool acknowledged);
}