namespace AeroLink.Web.Models;

/// <summary>Possible outcomes when a handler reports a problem with a bag (I4).</summary>
public enum ExceptionReportOutcome
{
    /// <summary>The problem was recorded as a new open exception.</summary>
    Reported,

    /// <summary>No tag was supplied after whitespace was removed.</summary>
    EmptyTag,

    /// <summary>The category is not one of <see cref="ExceptionCategories.All"/>.</summary>
    InvalidCategory,

    /// <summary>No comment was supplied after whitespace was removed.</summary>
    EmptyComment,

    /// <summary>The comment is longer than the allowed maximum.</summary>
    CommentTooLong,

    /// <summary>No bag in the manifest dataset has the supplied tag.</summary>
    NotFound,

    /// <summary>The tag belongs to a bag assigned to another departure flight.</summary>
    WrongFlight,

    /// <summary>The bag is already loaded, so it cannot be reported as a problem.</summary>
    AlreadyLoaded,

    /// <summary>The bag already has an open exception.</summary>
    AlreadyReported
}