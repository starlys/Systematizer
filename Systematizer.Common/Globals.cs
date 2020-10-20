using System;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    public static class Globals
    {
        //db info
        public static string DatabasePath;
        public static ConnectionWrapper Connection;
        public static BoxCache BoxCache = new BoxCache();

        //info read from settings
        public static bool AllowTasks;
        public static MultiDayChunkSet DayChunks;
        public static string[] PersonCustomLabels; //may be null or blank if not used
        public static CatCache AllCats;

        internal static readonly UIState UIState = new UIState();

        internal static readonly BoxEditingPool BoxEditingPool = new BoxEditingPool();

        /// <summary>
        /// UI layer must inject this
        /// </summary>
        public static IUIAction UIAction;

        /// <summary>
        /// UI notifications must call methods here
        /// </summary>
        public static readonly UIService UI = new UIService();

        public static readonly CommandCenter Commands = new CommandCenter();
    }
}
