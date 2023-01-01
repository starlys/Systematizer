using System.Collections.ObjectModel;

namespace Systematizer.WPF;

class PersonSearchVM : BaseBlockVM
{
    public class ResultItem : BaseVM
    {
        public long PersonId { get; set; }
        public string Name { get; set; }
    }

    //injected behaviors
    public readonly Action SearchRequested;
    public readonly Action<int> OpenRequested;

    /// <summary>
    /// Optionally implemented by code behind to return the control immediately prior to the results
    /// </summary>
    public Func<FrameworkElement> GetPreResultsControl { get; set; }

    public PersonSearchVM(Action<BaseBlockVM> gotFocusAction, Action searchRequested, Action<int> openRequested) : base(gotFocusAction)
    {
        OpenRequested = openRequested;
        SearchRequested = searchRequested;
        Results.CollectionChanged += (s, e) => NotifyChanged(nameof(EmptyMessageVisibility));
    }

    public Visibility EmptyMessageVisibility => ToVisibility(!Results.Any());

    public override string BlockTitle => "Search People";

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
    /// User entered flag - true means search also in notes; false means search only in names
    /// </summary>
    public bool IncludeDetailsCri
    {
        get => _includeDetailsCri;
        set { _includeDetailsCri = value; NotifyChanged(); }
    }

    public long[] CatIdCri { get; set; }

    public ObservableCollection<ResultItem> Results { get; set; } = new ObservableCollection<ResultItem>();
}
