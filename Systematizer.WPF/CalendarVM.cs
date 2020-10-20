using System;
using System.Windows.Media;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class CalendarVM : BaseBlockVM
    {
        public class LabelVM : BaseVM
        {
            public string Text { get; set; }
            public int Top { get; set; }
            public int Left { get; set; }
        }
        public class CellVM : LabelVM
        {
            public int Height { get; set; }
            public int Width { get; set; }
        }
        public class BarVM : CellVM
        {
            public Brush Background { get; set; }
            public long RowId;
        }

        //injected behavior to recreate VM contents for new size
        public Action<double> ControlResized;

        //injected behavior to open bar by mouse
        public Action<CalendarVM.BarVM> OpenRequested;

        /// <summary>
        /// setup model with no entries
        /// </summary>
        /// <param name="gotFocusAction"></param>
        public CalendarVM(Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
        {
        }

        public override string BlockTitle => "Calendar";

        double _canvasHeight;
        public double CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                _canvasHeight = value;
                NotifyChanged();
            }
        }

        public RangeObservableCollection<LabelVM> MonthNames { get; set; } = new RangeObservableCollection<LabelVM>();
        public RangeObservableCollection<CellVM> Days { get; set; } = new RangeObservableCollection<CellVM>();
        public RangeObservableCollection<BarVM> Bars { get; set; } = new RangeObservableCollection<BarVM>();
    }
}
