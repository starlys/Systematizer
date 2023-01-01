namespace Systematizer.Common.PersistentModel;

public abstract class SmallBox : BaseTable
{
    public long? ParentId { get; set; }
    public short TimeType { get; set; }
    public short Importance { get; set; }
    public short Visibility { get; set; }
    public string BoxTime { get; set; }
    public string DoneDate { get; set; }
    public short IsUnclass { get; set; }
    public string Title { get; set; }
    public string Duration { get; set; }
    public string PrepDuration { get; set; }
    public string RepeatInfo { get; set; }
}

public class Box : SmallBox
{
    const string INVALID_DURATION = "Duration/prep duration must be in the form of 15m, 2h, 3d";

    public string Notes { get; set; }
    public string RefDir { get; set; }
    public string RefFile { get; set; }
    public string Password { get; set; }
    public string RawEmail { get; set; }

    /// <summary>
    /// Return null or validation error message
    /// </summary>
    public string Validate()
    {
        bool durationOk = string.IsNullOrEmpty(Duration) || DateUtil.ParseDuration(Duration) != null;
        bool prepDurationOk = string.IsNullOrEmpty(PrepDuration) || DateUtil.ParseDuration(PrepDuration) != null;
        if (!durationOk || !prepDurationOk) return INVALID_DURATION;
        bool titleOk = !string.IsNullOrEmpty(Title);
        if (!titleOk) return "Title required";

        return null;
    }
}
