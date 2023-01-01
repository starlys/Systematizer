using Systematizer.Common.PersistentModel;

namespace Systematizer.Common;

/// <summary>
/// Box from database with extra parsed info. This is used for detail load and saves from the UI layer.
/// Also see CachedBox which is used for readonly access to lists of boxes to build the schedule.
/// </summary>
public class ExtBox
{
    /// <summary>
    /// The box from the database model
    /// </summary>
    public Box Box;

    /// <summary>
    /// null or repeat info
    /// </summary>
    public ParsedRepeatInfo Repeats;

    public List<LinkRecord> Links;

    /// <param name="links">optional (see DBUtil.LoadLinksFor)</param>
    public ExtBox(Box box, List<LinkRecord> links)
    {
        Box = box;
        Repeats = ParsedRepeatInfo.Build(box.RepeatInfo);
        Links = links ?? new List<LinkRecord>();
    }
}
