using System;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class SubjectVM : BaseBlockVM
    {
        public enum ChildrenStatus { No, YesPlaceholder, YesLoaded }

        public class RowVM : BaseVM
        {
            public RowVM Parent;

            /// <summary>
            /// If null, this is a placeholder node that exists to fill out a level prior to loading
            /// </summary>
            public CachedBox Persistent;

            public RangeObservableCollection<RowVM> Children { get; set; } = new RangeObservableCollection<RowVM>();

            public RowVM(CachedBox box, RowVM parent)
            {
                Persistent = box;
                Parent = parent;
            }

            ChildrenStatus _status;
            /// <summary>
            /// Gets or sets status and clears children; in the case of setting a placeholder, this will add the placeholder child node
            /// </summary>
            public ChildrenStatus Status
            {
                get => _status;
                set
                {
                    _status = value;
                    Children.Clear();
                    if (_status == ChildrenStatus.YesPlaceholder)
                       Children.Add(new RowVM(null, this));
                    NotifyChanged();
                    NotifyChanged("IsExpanded");
                }
            }

            public bool IsExpanded
            {
                get => _status == ChildrenStatus.YesLoaded;
                set
                {
                    //no direct effect when changed through binding; instead the expand handler sets Status
                }
            }

            /// <summary>
            /// Tell the VM that the persistent CachedBox changed
            /// </summary>
            public void Touch()
            {
                NotifyChanged("Title");
            }

            public string Title => Persistent?.Title ?? "-";
        }

        //injected action to remember last focused box
        public Action<RowVM> ItemGotFocus;

        //injected action to load or forget children of an item (bool is whether expanded or collapsed)
        public Action<RowVM, bool> ItemExpanded;

        //injected action to handle Enter pressed
        public Action OpenRequested;

        /// <summary>
        /// Set up model with no agenda entries
        /// </summary>
        public SubjectVM(Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
        {
        }

        public override string BlockTitle => "Subjects";

        public RangeObservableCollection<RowVM> RootRows { get; set; } = new RangeObservableCollection<RowVM>();

    }
}
