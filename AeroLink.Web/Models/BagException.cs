namespace AeroLink.Web.Models;

/// <summary>A problem reported against a bag, awaiting or holding a supervisor decision.</summary>
public class BagException
{
    /// <summary>Gets or sets the unique identifier for this reported exception.</summary>
    public int ExceptionId { get; set; }
    /// <summary>Gets or sets the identifier of the bag with the reported problem.</summary>
    public int BagId { get; set; }
    /// <summary>Gets or sets the identifier of the flight assigned to the bag.</summary>
    public int FlightId { get; set; }

    /// <summary>Problem type, e.g. MissingBag or DamagedBag.</summary>
    public string Category { get; set; } = "";

    /// <summary>Description or comment recorded when the problem was reported.</summary>
    public string Description { get; set; } = "";

    /// <summary>Gets or sets whether the exception is still open or has been resolved.</summary>
    public ExceptionStatus Status { get; set; } = ExceptionStatus.Open;

    /// <summary>Supervisor's recorded reason for the decision.</summary>
    public string? DecisionReason { get; set; }

    /// <summary>When the problem was reported (UTC). Needed by user story I4.</summary>
    public DateTime? ReportedAtUtc { get; set; }

    /// <summary>Gets or sets the bag associated with this exception.</summary>
    public Bag? Bag { get; set; }
}