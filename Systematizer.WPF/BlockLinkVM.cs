using System;
using System.Linq;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class BlockLinkVM : BaseVM
    {
        public class ItemVM : BaseVM
        {
            public LinkType Link;
            public long OtherId;

            //injected behavior
            public Action<ItemVM> LinkClicked;

            /// <summary>
            /// ctor sets all VM properties except behaviors
            /// </summary>
            public ItemVM(LinkRecord l) : this(l.Link, l.OtherId, l.Description)
            {
            }

            /// <summary>
            /// ctor sets all VM properties except behaviors
            /// </summary>
            public ItemVM(LinkType link, long toId, string description)
            {
                Link = link;
                OtherId = toId;
                Description = Prefix + description;
            }

            string _description;
            public string Description
            {
                get => _description;
                set { _description = value; NotifyChanged(); }
            }

            public string Prefix
            {
                get
                {
                    if (Link == LinkType.FromBoxToChildBox) return "Sub-item: ";
                    if (Link == LinkType.FromBoxToParentBox) return "Parent item: ";
                    if (Link == LinkType.FromBoxToPerson || Link == LinkType.FromPersonToPerson) return "Linked person: ";
                    if (Link == LinkType.FromPersonToBox) return "Linked task/note: ";
                    return "";
                }
            }
        }

        public RangeObservableCollection<ItemVM> Items { get; set; } = new RangeObservableCollection<ItemVM>();

        public Visibility WholeVisibility => ToVisibility(Items.Any());

        /// <summary>
        /// Call this after modifying Items
        /// </summary>
        public void Touch()
        {
            NotifyChanged("WholeVisibility");
        }
    }
}
