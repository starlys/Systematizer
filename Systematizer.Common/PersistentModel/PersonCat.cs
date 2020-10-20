using System;
using System.Collections.Generic;

namespace Systematizer.Common.PersistentModel
{
    public partial class PersonCat : BaseTable
    {
        public long PersonId { get; set; }
        public long CatId { get; set; }
    }
}
