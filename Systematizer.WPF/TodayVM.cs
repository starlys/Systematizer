using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Systematizer.WPF;

/// <summary>
/// Model for Today view (also used for tomorrow)
/// </summary>
class TodayVM : BaseListBlockVM
{
    public class ChunkVM : BaseVM
    {
        string _title;
        public string Title
        {
            get => _title; 
            set
            {
                _title = value;
                NotifyChanged();
            }
        }

        Brush _dragTargetColor = UIGlobals.TRANSPARENT_BRUSH;
        public Brush DragTargetColor
        {
            get => _dragTargetColor;
            set
            {
                _dragTargetColor = value;
                NotifyChanged();
            }
        }

        public ObservableCollection<BoxPreviewVM> Items { get; set; } = new ObservableCollection<BoxPreviewVM>();

        public bool ContainsBoxId(long id) => Items.Any(i => i.Persistent.Box.RowId == id);
    }

    /// <summary>
    /// true for today, false for tomorrow or any other day
    /// </summary>
    public readonly bool IsToday;

    //actions injected by controller
    public Action RequestAddChunk;
    public Action<BoxDragInfo, ChunkVM> DropOnChunkRequested;
    public Action<int> ChunkGotFocus, ChunkLostFocus;

    //actions injected by view
    //public Action FocusFirstChunk

    /// <summary>
    /// Set up model with no agenda entries
    /// </summary>
    /// <param name="date">YYYYMMDD</param>
    public TodayVM(bool isToday, string date, Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
    {
        IsToday = isToday;
        string title = DateUtil.ToReadableDate(date, includeDOW: true);
        if (isToday) title = "Today: " + title;
        _blockTitle = title;
    }

    readonly string _blockTitle;
    public override string BlockTitle => _blockTitle;

    public ObservableCollection<ChunkVM> Chunks { get; set; } = new ObservableCollection<ChunkVM>();

    string _newChunkTitle;
    /// <summary>
    /// should be bound with update on lostfocus; creates chunk when set
    /// </summary>
    public string NewChunkTitle
    {
        get => _newChunkTitle;
        set
        {
            _newChunkTitle = value;
            NotifyChanged();
            if (!string.IsNullOrEmpty(_newChunkTitle))
                RequestAddChunk?.Invoke();
        }
    }

    public bool ContainsBoxId(long id) => Chunks.Any(i => i.ContainsBoxId(id));
}
