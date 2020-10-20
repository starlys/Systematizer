using System;
using System.Collections.Generic;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    /// <summary>
    /// Box from database with extra parsed info. This is used for detail load and saves from the UI layer.
    /// Also see CachedBox which is used for readonly access to lists of boxes to build the schedule.
    /// </summary>
    public class ExtPerson
    {
        /// <summary>
        /// The person from the database model
        /// </summary>
        public Person Person;

        public List<LinkRecord> Links;

        public long[] SelectedCatIds;

        /// <param name="links">optional (see DBUtil.LoadLinksFor)</param>
        /// <param name="selectedCatIds">optional</param>
        public ExtPerson(Person person, List<LinkRecord> links, long[] selectedCatIds)
        {
            Person = person;
            Links = links ?? new List<LinkRecord>();
            SelectedCatIds = selectedCatIds ?? new long[0];
        }
    }
}
