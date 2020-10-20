using System;
using System.Collections.Generic;

namespace Systematizer.Common.PersistentModel
{
    public partial class Word : BaseTable
    {
        public short Kind { get; set; }
        public short IsDetail { get; set; }
        public long ParentId { get; set; }
        public string Word8 { get; set; }
    }
}
