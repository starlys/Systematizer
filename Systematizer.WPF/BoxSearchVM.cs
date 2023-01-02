using System.Collections.ObjectModel;

namespace Systematizer.WPF;

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

    /// <summary>
    /// When in done mode, the minimum done date YYYYMMDD 
    /// </summary>
    public DateVM DoneSinceCri { get; set; } = new DateVM();

    public ObservableCollection<BoxPreviewVM> Results { get; set; } = new ObservableCollection<BoxPreviewVM>();
}
