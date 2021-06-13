using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class BoxSearchVM : BaseBlockVM
    {
        //injected behaviors
        public readonly Func<CommandCenter.Item, bool> HandleCommand;
        public readonly Action SearchRequested;

        /// <summary>
        /// Optionally implemented by code behind to return the control immediately prior to the results
        /// </summary>
        public Func<FrameworkElement> GetPreResultsControl { get; set; }

        public readonly bool DoneMode;

        public BoxSearchVM(Action<BaseBlockVM> gotFocusAction, bool doneMode, Func<CommandCenter.Item, bool> handleCommand, Action searchRequested) : base(gotFocusAction)
        {
            HandleCommand = handleCommand;
            DoneMode = doneMode;
            SearchRequested = searchRequested;
            DoneSinceCri.IsEditMode = true;
            Results.CollectionChanged += (s, e) => NotifyChanged(nameof(EmptyMessageVisibility));
        }

        public Visibility EmptyMessageVisibility => ToVisibility(!Results.Any());
        public Visibility DoneSinceVisibility => ToVisibility(DoneMode);

        public override string BlockTitle => DoneMode ? "Done Tasks" : "Search Tasks/Notes";

        string _termCri;
        /// <summary>
        /// User entered search term
        /// </summary>
        public string TermCri
        {
            get => _termCri;
            set { _termCri = value; NotifyChanged(); }
        }

        bool _includeDetailsCri;
        /// <summary>
        /// User entered flag - true means search also in notes; false means search only in titles
        /// </summary>
        public bool IncludeDetailsCri
        {
            get => _includeDetailsCri;
            set { _includeDetailsCri = value; NotifyChanged(); }
        }

        /// <summary>
        /// When in done mode, the minimum done date YYYYMMDD 
        /// </summary>
        public DateVM DoneSinceCri { get; set; } = new DateVM();

        public ObservableCollection<BoxPreviewVM> Results { get; set; } = new ObservableCollection<BoxPreviewVM>();
    }
}
