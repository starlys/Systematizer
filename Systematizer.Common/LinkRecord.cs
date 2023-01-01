namespace Systematizer.Common;

/// <summary>
/// A link between records owned by the "From" side
/// </summary>
public class LinkRecord
{
    public LinkType Link;
    public long OtherId;
    
    /// <summary>
    /// title or name of the "to" record
    /// </summary>
    public string Description;
}
