using System;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    /// <summary>
    /// Version of Box for in-memory caching and list views (omits potentially large cols or those unneeded for lists/outlines)
    /// This is used as a readonly viewmodel.
    /// </summary>
    public class CachedBox : SmallBox
    {
        /// <summary>
        /// truncated notes for memory savings
        /// </summary>
        public string SmallNotes { get; set; }

        /// <summary>
        /// Null if non-repeating or the RepeatInfo parsed out 
        /// </summary>
        public ParsedRepeatInfo Repeats { get; set; }

        /// <summary>
        /// Truncate SmallNotes to 100 chars
        /// </summary>
        internal void TruncateSmallNotes()
        {
            if (SmallNotes != null && SmallNotes.Length > 100)
                SmallNotes = SmallNotes.Substring(0, 100);
            if (SmallNotes != null)
                SmallNotes = SmallNotes.Replace("\r\n", " / ");
        }
    }
}
