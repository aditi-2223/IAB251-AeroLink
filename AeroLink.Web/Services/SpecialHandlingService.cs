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

    private static string ResolveCategory(Bag bag) =>
        string.IsNullOrWhiteSpace(bag.HandlingType) ? UnspecifiedCategory : bag.HandlingType.Trim();

    private static string ResolveInstruction(Bag bag, string fallback) =>
        string.IsNullOrWhiteSpace(bag.HandlingInstruction) ? fallback : bag.HandlingInstruction.Trim();
}