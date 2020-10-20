using System;
using System.Collections.Generic;

namespace Systematizer.Common.PersistentModel
{
    public partial class Cat : BaseTable
    {
        public string Name { get; set; }
        public long? ParentId { get; set; }
    }
}
