using System;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// viewmodel for links panel (shown outside the blocks)
    /// </summary>
    class RecordLinkVM : BaseVM
    {
        public class ItemVM : BaseVM
        {
            /// <summary>
            /// If true, this entry doesn't depend on the active block
            /// </summary>
            public bool IsSticky;

            string _buttonText;
            public string ButtonText
            {
                get => _buttonText;
                set
                {
                    _buttonText = value;
                    NotifyChanged();
                }
            }

            string _description;
            public string Description
            {
                get => _description;
                set
                {
                    _description = value;
                    NotifyChanged();
                }
            }

            public LinkInstruction Command;
        }

        /// <summary>
        /// Injected behavior to create/remove a link
        /// </summary>
        public Action<ItemVM> ActionRequested;

        public RangeObservableCollection<ItemVM> Items { get; set; } = new RangeObservableCollection<ItemVM>();

        string _instructions;
        public string Instructions
        {
            get => _instructions;
            set
            {
                _instructions = value;
                NotifyChanged();
            }
        }

        bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                NotifyChanged();
                NotifyChanged(nameof(WholeVisibility));
            }
        }

        public Visibility WholeVisibility => ToVisibility(IsActive);
    }
}
