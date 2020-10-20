using System;

namespace Systematizer.Common.PersistentModel
{
    public partial class Person : BaseTable
    {
        public string Name { get; set; }
        public string MainPhone { get; set; }
        public string MainEmail { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public string Custom1 { get; set; }
        public string Custom2 { get; set; }
        public string Custom3 { get; set; }
        public string Custom4 { get; set; }
        public string Custom5 { get; set; }

        /// <summary>
        /// Return null or validation error message
        /// </summary>
        public string Validate()
        {
            bool nameOk = !string.IsNullOrEmpty(Name);
            if (!nameOk) return "Name required";

            return null;
        }
    }
}
