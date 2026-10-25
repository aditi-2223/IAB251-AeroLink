using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="ISpecialHandlingService"/>
public class SpecialHandlingService : ISpecialHandlingService
{
    private const string StandardInstruction = "Standard handling.";
    private const string DefaultSpecialInstruction =
        "Follow the special-handling procedure for this bag type and ask your supervisor if unsure.";
    private const string UnspecifiedCategory = "Special";
    private const string LoadingAllowedMessage = "Loading may be confirmed.";

    /// <inheritdoc />
    public SpecialHandlingInfo GetInfo(Bag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);

        return bag.IsSpecial
            ? new SpecialHandlingInfo(true, ResolveCategory(bag), ResolveInstruction(bag, DefaultSpecialInstruction))
            : new SpecialHandlingInfo(false, HandlingTypes.Standard, ResolveInstruction(bag, StandardInstruction));
    }

    /// <inheritdoc />
    public LoadingDecision CanConfirmLoading(Bag bag, bool acknowledged)
    {
        var info = GetInfo(bag);

        if (info.RequiresAcknowledgement && !acknowledged)
            return LoadingDecision.Reject($"Acknowledge the {info.Category} handling instruction before loading this bag.");

        return LoadingDecision.Allow(LoadingAllowedMessage);
    }

    /// <summary>Gets the trimmed handling category to display for a special bag.</summary>
    /// <param name="bag">The bag whose handling category is being read.</param>
    /// <returns>The bag's trimmed category, or <c>Special</c> when the category is blank.</returns>
    private static string ResolveCategory(Bag bag) =>
        string.IsNullOrWhiteSpace(bag.HandlingType) ? UnspecifiedCategory : bag.HandlingType.Trim();

    /// <summary>Gets the trimmed handling instruction, using a supplied default when it is blank.</summary>
    /// <param name="bag">The bag whose instruction is being read.</param>
    /// <param name="fallback">The instruction to use when the bag has no instruction.</param>
    /// <returns>The bag's trimmed instruction, or <paramref name="fallback"/>.</returns>
    private static string ResolveInstruction(Bag bag, string fallback) =>
        string.IsNullOrWhiteSpace(bag.HandlingInstruction) ? fallback : bag.HandlingInstruction.Trim();
}