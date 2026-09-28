namespace AeroLink.Web.Models;

/// <summary>A problem reported against a bag, awaiting or holding a supervisor decision.</summary>
public class BagException
{
    public int ExceptionId { get; set; }
    public int BagId { get; set; }
    public int FlightId { get; set; }

    /// <summary>Problem type, e.g. MissingBag or DamagedBag.</summary>
    public string Category { get; set; } = "";

    /// <summary>Description or comment recorded when the problem was reported.</summary>
    public string Description { get; set; } = "";

    public ExceptionStatus Status { get; set; } = ExceptionStatus.Open;

    /// <summary>Supervisor's recorded reason for the decision.</summary>
    public string? DecisionReason { get; set; }

    /// <summary>When the problem was reported (UTC). Needed by user story I4.</summary>
    public DateTime? ReportedAtUtc { get; set; }

    public Bag? Bag { get; set; }
}