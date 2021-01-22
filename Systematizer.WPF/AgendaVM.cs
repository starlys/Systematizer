using System;
using System.Windows;
using System.Windows.Media;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class AgendaVM : BaseBlockVM
    {
        public class RowVM : BaseVM
        {
            readonly bool IsHeading;
            public AgendaEntry AgendaEntry { get; private set; }

            /// <summary>
            /// Midnight on the date for headings, else the box time
            /// </summary>
            public string Time { get; private set; }

            public RowVM(string headingDate, string readableDate)
            {
                if (headingDate.Length != 8) throw new Exception("Call error");
                Time = headingDate + "0000";
                MainText = readableDate;
                IsHeading = true;
            }

            public RowVM(AgendaEntry ae)
            {
                AgendaEntry = ae;
                Time = ae.Time;
                MainText = ae.Box.Title;
                TimeDisplay = ae.Box.TimeType >= Constants.TIMETYPE_MINUTE ? DateUtil.ToReadableTime(ae.Time) : "----";
            }

            public Visibility HeadingVisibility => ToVisibility(IsHeading);
            public Visibility BoxVisibility => ToVisibility(!IsHeading);

            /// <summary>
            /// null or highlight color
            /// </summary>
            public Brush HorzHighlightColor
            {
                get
                {
                    if (AgendaEntry == null || AgendaEntry.HighlightColor < 0) return UIGlobals.BOX_TITLE_BRUSH;
                    return UIGlobals.HIGHLIGHT_COLORS[AgendaEntry.HighlightColor];
                }
            }

            /// <summary>
            /// highligh flags for this row based on whether a different highlighted box has a duration that extends through this row.
            /// Example: if highlight color 2 is to be lit up on this row, then VertHighlights[2]==true.
            /// Array elements set by controller after list is built.
            /// </summary>
            public Brush[] VertHighlightColor { get; set; } = new Brush[Constants.NHIGHLIGHT_COLORS];

            public Brush VertHighlightColor0 => VertHighlightColor[0];
            public Brush VertHighlightColor1 => VertHighlightColor[1];
            public Brush VertHighlightColor2 => VertHighlightColor[2];
            public Brush VertHighlightColor3 => VertHighlightColor[3];
            public Brush VertHighlightColor4 => VertHighlightColor[4];
            public Brush VertHighlightColor5 => VertHighlightColor[5];

            string _timeDisplay;
            public string TimeDisplay
            {
                get => _timeDisplay;
                set
                {
                    _timeDisplay = value;
                    NotifyChanged();
                }
            }

            string _mainText;
            /// <summary>
            /// Either heading or box title
            /// </summary>
            public string MainText
            {
                get => _mainText;
                set
                {
                    _mainText = value;
                    NotifyChanged();
                }
            }
        }

        //injected action to show more weeks on the agenda
        public Action MoreRequested;

        //injected action to open box
        public Action<FrameworkElement, RowVM> MouseOpenRequested;

        //injected action to remember last focused agenda entry
        public Action<int> ItemGotFocus;

        /// <summary>
        /// Set up model with no agenda entries
        /// </summary>
        public AgendaVM(Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
        {
        }

        public override string BlockTitle => "Agenda";

        public RangeObservableCollection<RowVM> Rows { get; set; } = new RangeObservableCollection<RowVM>();

        public Visibility RequestMoreVisibility => ToVisibility(AllowRequestMore);

        int _boxVisibilityIncluded = Constants.VISIBILITY_PLANAROUND;
        /// <summary>
        /// The Box.Visibility numbers (this number and above) which are included in the agenda view
        /// </summary>
        public int BoxVisibilityIncluded
        {
            get => _boxVisibilityIncluded;
            set
            {
                _boxVisibilityIncluded = value;
                NotifyChanged();
                NotifyChanged("SliderToolTip");
            }
        }

        bool _allowRequestMore;
        public bool AllowRequestMore
        {
            get => _allowRequestMore;
            set
            {
                _allowRequestMore = value;
                NotifyChanged();
                NotifyChanged("RequestMoreVisibility");
            }
        }

        public string SliderToolTip
        {
            get 
            {
                return BoxVisibilityIncluded switch
                {
                    3 => "Show highlights only",
                    2 => "Show default",
                    1 => "Show all tasks (except low-clutter)",
                    _ => "Show everything"
                };
            }
        }
    }
}
