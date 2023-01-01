namespace Systematizer.Common;

public static class Constants
{
    public const short TIMETYPE_NONE = 0, TIMETYPE_APPROX = 1, TIMETYPE_DAY = 2, TIMETYPE_MINUTE = 3;

    public const short IMPORTANCE_LOW = 0, IMPORTANCE_NORMAL = 1, IMPORTANCE_HIGH = 2;

    public const short VISIBILITY_LOWCLUTTER = 0, VISIBILITY_NORMAL = 1, VISIBILITY_PLANAROUND = 2, VISIBILITY_HIGHLIGHT = 3;

    /// <summary>
    /// shortcut chars for day of week entry: sunday through sat
    /// </summary>
    public static readonly char[] DAYOFWEEK_CHARS = new[] {'S','M','T','W','H','F','A'};

    public static readonly KeyValuePair<short, string>[] TIMETYPE_UICHOICES = new[]
    {
        new KeyValuePair<short, string>(TIMETYPE_NONE, "Not scheduled"),
        new KeyValuePair<short, string>(TIMETYPE_APPROX, "Approximate"),
        new KeyValuePair<short, string>(TIMETYPE_DAY, "Day"),
        new KeyValuePair<short, string>(TIMETYPE_MINUTE, "Exact time")
    };

    public static readonly KeyValuePair<short, string>[] IMPORTANCE_UICHOICES = new[]
    {
        new KeyValuePair<short, string>(IMPORTANCE_LOW, "Optional"),
        new KeyValuePair<short, string>(IMPORTANCE_NORMAL, "Normal"),
        new KeyValuePair<short, string>(IMPORTANCE_HIGH, "Keep-alive")
    };

    public static readonly KeyValuePair<short, string>[] VISIBILITY_UICHOICES = new[]
    {
        new KeyValuePair<short, string>(VISIBILITY_LOWCLUTTER, "Low clutter"),
        new KeyValuePair<short, string>(VISIBILITY_NORMAL, "Normal visibility"),
        new KeyValuePair<short, string>(VISIBILITY_PLANAROUND, "Plan around"),
        new KeyValuePair<short, string>(VISIBILITY_HIGHLIGHT, "Highlight")
    };

    /// <summary>
    /// The color choices are declared in the UI layer
    /// </summary>
    public const int NHIGHLIGHT_COLORS = 6;
}
