using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    /// <summary>
    /// Cache instance can be rebuilt after each change
    /// </summary>
    public class CatCache
    {
        /// <summary>
        /// Can be used for VM binding
        /// </summary>
        public class Item : IComparable
        {
            public long RowId;
            public string Name { get; set; }
            public Item Parent;
            public List<Item> Children { get; set; } //null ok

            public int CompareTo(object obj)
            {
                var i2 = obj as Item;
                return (Name ?? "").CompareTo(i2?.Name);
            }
        }

        public List<Item> Roots;

        public CatCache(IEnumerable<Cat> records)
        {
            var remaining = new List<Cat>(records);
            var roots = new List<Item>();
            var all = new List<Item>();
            int noCrashNum = 0;
            while (remaining.Count > 0 && ++noCrashNum < 100)
            {
                for (int i = remaining.Count - 1; i >= 0; --i)
                {
                    var c = remaining[i];
                    if (c.ParentId == null)
                    {
                        var it = new Item { RowId = c.RowId, Name = c.Name };
                        all.Add(it);
                        roots.Add(it);
                        remaining.RemoveAt(i);
                    }
                    else
                    {
                        var p = all.FirstOrDefault(r => r.RowId == c.ParentId);
                        if (p != null)
                        {
                            var it = new Item { RowId = c.RowId, Name = c.Name, Parent = p };
                            all.Add(it);
                            if (p.Children == null) p.Children = new List<Item>();
                            p.Children.Add(it);
                            remaining.RemoveAt(i);
                        }
                    }
                }
            }
            SortRecursive(roots);
            Roots = roots;
        }

        /// <summary>
        /// Get readable lineage of a category. Since it is meant to be used for leaf nodes only, the bool arg is used
        /// to force a null return value if the node is non-leaf.
        /// </summary>
        public string GetReadableLineage(long catId, bool nullIfHasChildren)
        {
            var it = Find(catId);
            if (it == null) return "";
            if (nullIfHasChildren && it.Children != null && it.Children.Any()) return null;
            var s = it.Name;

            while (it.Parent != null)
            {
                it = it.Parent;
                s = it.Name + " > " + s;
            }
            return s;
        }

        public Item Find(long catId)
        {
            return FindRecursive(Roots, catId);
        }

        /// <summary>
        /// Get an array consisting of at least the id of the given item, plus all descendant ids;
        /// if unknown id returns empty array
        /// </summary>
        public long[] GetDescendantIds(long catId)
        {
            var it = Find(catId);
            if (it == null) return new long[0];
            var ids = new List<long>();
            GetDescendantIdsRecursive(ids, it);
            return ids.ToArray();
        }

        void GetDescendantIdsRecursive(List<long> ids, Item it)
        {
            ids.Add(it.RowId);
            if (it.Children != null)
                foreach (var c in it.Children)
                    GetDescendantIdsRecursive(ids, c);
        }

        Item FindRecursive(List<Item> list, long catId)
        {
            foreach (var it in list)
            {
                if (it.RowId == catId) return it;
                if (it.Children != null)
                {
                    var it2 = FindRecursive(it.Children, catId);
                    if (it2 != null) return it2;
                }
            }
            return null;
        }

        void SortRecursive(List<Item> list)
        {
            list.Sort();
            foreach (var it in list)
                if (it.Children != null)
                    SortRecursive(it.Children);
        }
    }
}
