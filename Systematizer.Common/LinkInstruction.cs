using System;

namespace Systematizer.Common
{
    /// <summary>
    /// Information needed to create or remove a link between boxes and/or persons
    /// </summary>
    public class LinkInstruction
    {
        public LinkType Link;
        public long FromId, ToId;
        public bool IsRemove; //true to remove link, false to create link
        public string ToDescription; //name/title of ToId
    }
}
