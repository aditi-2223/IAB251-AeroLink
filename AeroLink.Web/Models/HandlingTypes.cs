namespace AeroLink.Web.Models;

/// <summary>
/// Names of the handling categories used in the baggage manifest dataset.
/// </summary>
/// <remarks>
/// Values must match the <c>HandlingType</c> column of the supplied manifest exactly (comparison is
/// case-insensitive). Any category other than <see cref="Standard"/> is treated as special handling.
/// </remarks>
public static class HandlingTypes
{
    /// <summary>A normal bag with no special requirements.</summary>
    public const string Standard = "Standard";

    /// <summary>A bag containing fragile items.</summary>
    public const string Fragile = "Fragile";

    /// <summary>A bag too large for the standard baggage belt.</summary>
    public const string Oversized = "Oversized";

    /// <summary>A wheelchair or mobility aid.</summary>
    public const string Wheelchair = "Wheelchair";

    /// <summary>Sporting equipment such as skis, bicycles or golf clubs.</summary>
    public const string SportingEquipment = "SportingEquipment";
}