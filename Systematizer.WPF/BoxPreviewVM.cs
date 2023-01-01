using System.Windows.Media;

namespace Systematizer.WPF;

class BoxPreviewVM : BaseVM
{
    readonly string ContextDate; //YYMMDD for the date showing in the parent of this view, or null to assume always is context date

    //view can also bind to Persistent.Box.Title and Persistent.Box.SmallNotes
    public AgendaEntry Persistent { get; set; }

    public string DisplayTime { get; set; }

    //injected handlers, called from code-behind
    public Action<BoxPreviewVM, FrameworkElement> TimeClicked; //used to focus box title
    public Action<BoxPreviewVM, FrameworkElement> DragStartRequested;
    public Action<BoxPreviewVM> MouseOpenRequested;
    public Action<BoxPreviewVM> GotFocus { get; private set; }
    public Action<BoxDragInfo, BoxPreviewVM> DropUnderBoxRequested;

    public BoxPreviewVM(AgendaEntry persistent, string contextDate, Action<BoxPreviewVM> gotFocusAction)
    {
        GotFocus = gotFocusAction;
        Persistent = persistent;
        ContextDate = contextDate;
        InitializeFromPersistent();
    }

    public override void InitializeFromPersistent()
    {
        var box = Persistent.Box;
        bool isDaySpecific = box.TimeType >= Constants.TIMETYPE_DAY;
        if (isDaySpecific)
        {
            bool isContextDate = ContextDate == null || (box.BoxTime != null && box.BoxTime.StartsWith(ContextDate));
            if (isContextDate)
            {
                bool isMinuteSpecific = box.TimeType >= Constants.TIMETYPE_MINUTE;
                if (isMinuteSpecific)
                    DisplayTime = DateUtil.ToReadableTime(box.BoxTime);
                else
                    DisplayTime = "----";
            }
            else
                DisplayTime = "(Past)";
        }
        else DisplayTime = "---";
    }

    Brush _dragTargetColor = UIGlobals.TRANSPARENT_BRUSH;
    /// <summary>
    /// This is used only by parent items' XAML, not by BoxPreviewVM itself
    /// </summary>
    public Brush DragTargetColor
    {
        get => _dragTargetColor;
        set
        {
            _dragTargetColor = value;
            NotifyChanged();
        }
    }
}
