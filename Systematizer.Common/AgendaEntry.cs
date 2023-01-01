namespace Systematizer.Common;

/// <summary>
/// A Box record as it appears on the agenda. For repeating tasks, there are many of these per CachedBox, while for other
/// tasks there is just 1
/// Note this is used as a readonly view model too.
/// </summary>
public class AgendaEntry
{
    /// <summary>
    /// YYYYMMDDHHMM when it should appear on agenda
    /// </summary>
    public string Time { get; set; }

    /// <summary>
    /// The underlying record
    /// </summary>
    public CachedBox Box { get; set; }

    /// <summary>
    /// If nonnull, the local time when prep and/or start time reminders should be given; after reminder given
    /// these are cleared so the user won't get the same reminder twice
    /// </summary>
    public DateTime? PendingPrepReminder, PendingReminder;

    /// <summary>
    /// -1 if no highlight
    /// </summary>
    public int HighlightColor = -1;

    /// <summary>
    /// YYYYMMDDHHMM when highlight ends on agenda, or null if no highlight
    /// </summary>
    public string HighlightEndTime;
}
