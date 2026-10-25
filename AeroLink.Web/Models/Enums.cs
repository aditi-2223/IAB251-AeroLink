namespace AeroLink.Web.Models;

/// <summary>Shows the current stage of baggage loading for a flight.</summary>
public enum LoadingStatus { NotStarted, InProgress, Completed }

/// <summary>Records the loading result for one bag in a manifest.</summary>
public enum BagOutcome { Pending, Loaded, ApprovedNotToLoad }

/// <summary>Shows whether a reported baggage exception still needs action.</summary>
public enum ExceptionStatus { Open, Resolved }