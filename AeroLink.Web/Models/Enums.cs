namespace AeroLink.Web.Models;

/// <summary>Lifecycle of a flight's baggage loading operation.</summary>
public enum LoadingStatus { NotStarted, InProgress, Completed }

/// <summary>Recorded outcome for a single bag on the manifest.</summary>
public enum BagOutcome { Pending, Loaded, ApprovedNotToLoad }

/// <summary>State of a reported baggage exception.</summary>
public enum ExceptionStatus { Open, Resolved }