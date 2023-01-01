#pragma warning disable CA2211 // Non-constant fields should not be visible

namespace Systematizer.Common;

public static class Globals
{
    //db info
    public static string DatabasePath; //null when db closed
    public static ConnectionWrapper Connection;
    public static BoxCache BoxCache = new();

    //info read from settings
    public static bool AllowTasks;
    public static MultiDayChunkSet DayChunks;
    public static string[] PersonCustomLabels; //may be null or blank if not used
    public static CatCache AllCats;

    internal static readonly UIState UIState = new();

    internal static readonly BoxEditingPool BoxEditingPool = new();

    /// <summary>
    /// UI layer must inject this
    /// </summary>
    public static IUIAction UIAction;

    public static readonly CommandCenter Commands = new();
}
