using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class CalendarController : ListBlockController
    {
        static readonly int BARHEIGHT = 16, WEEKHEIGHT = BARHEIGHT * Constants.NHIGHLIGHT_COLORS;

        public CalendarVM VM { get; private set; }

        public override BaseBlockVM GenericVM => VM;

        double Width;

        public CalendarController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested)
            : base(blockGotFocusHandler, collapseRequested)
        {
            VM = new CalendarVM(VMGotFocus);

            //inject VM behaviors
            VM.ControlResized = width =>
            {
                Width = width;
                RefreshDefinitely();
            };
            VM.OpenRequested = barVM =>
            {
                try
                {
                    var ebox = Globals.UI.LoadBoxForEditing(barVM.RowId);
                    if (ebox != null) UIGlobals.Do.AddBoxToEditStack(ebox);
                }
                catch { }
            };
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
            return true;
        }

        public override void Refresh(BoxEditingPool.Item changes)
        {
            if (changes != null && changes.IsAgendaChanged && Width > 100) 
                RefreshDefinitely();
        }

        void RefreshDefinitely()
        { 
            VM.Bars.Clear();
            VM.Days.Clear();
            VM.MonthNames.Clear();

            //get highlight tasks
            var boxes = Globals.BoxCache.GetAgenda().Where(ae => ae.Box.Visibility == Constants.VISIBILITY_HIGHLIGHT).ToArray();

            //start and end dates - show at least 6 months, and at least 1 month after last highlight start
            DateTime dStart = DateTime.Today;
            dStart = dStart.AddDays(0 - (int)dStart.DayOfWeek); //go to sunday
            DateTime dEnd = dStart.AddMonths(6);
            if (boxes.Any())
            {
                DateTime lastBoxTime = DateUtil.ToDateTime(boxes.Last().Box.BoxTime) ?? dStart;
                lastBoxTime = lastBoxTime.AddMonths(1);
                if (lastBoxTime > dEnd) dEnd = lastBoxTime.Date;
            }
            dEnd = dEnd.AddDays(6 - (int)dEnd.DayOfWeek); //go to sunday

            //create day cells and month names
            var days = new List<CalendarVM.CellVM>(300);
            var months = new List<CalendarVM.LabelVM>(10);
            int lastShownMonthNo = dStart.Month;
            int dow = -1, maxWeekNo = 0;
            double dayWidth = Width / 8;
            for (DateTime d = dStart; d <= dEnd; d = d.AddDays(1))
            {
                if (++dow == 7) { dow = 0; ++maxWeekNo; }
                if (d.Month != lastShownMonthNo)
                {
                    lastShownMonthNo = d.Month;
                    months.Add(new CalendarVM.LabelVM
                    {
                        Left = 0,
                        Top = WEEKHEIGHT * maxWeekNo + 20,
                        Text = d.ToString("MMM")
                    });
                }
                days.Add(new CalendarVM.CellVM
                {
                    Top = WEEKHEIGHT * maxWeekNo,
                    Height = WEEKHEIGHT,
                    Left = (int)(dayWidth * (dow + 1)),
                    Width = (int)dayWidth,
                    Text = d.Day.ToString()
                });
            }
            VM.Days.AddRange(days);
            VM.MonthNames.AddRange(months);
            VM.CanvasHeight = (maxWeekNo + 1) * WEEKHEIGHT;

            //create bars
            var bars = new List<CalendarVM.BarVM>();
            foreach (var box in boxes)
            {
                DateTime? d0 = DateUtil.ToDateTime(box.Box.BoxTime);
                if (d0 == null) continue;
                DateTime d1 = DateUtil.AddDuration(d0.Value, box.Box.Duration);
                double totalDays0 = d0.Value.Subtract(dStart).TotalDays; //from calendar beginning, can be negative
                double totalDays1 = d1.Subtract(dStart).TotalDays;
                int weekNo0 = (int)totalDays0 / 7;
                int weekNo1 = (int)totalDays1 / 7;
                double dow0 = totalDays0 % 7, dow1 = totalDays1 % 7; //for example 1.5 means mid-monday
                for (int weekNo = weekNo0; weekNo <= weekNo1; ++weekNo)
                {
                    if (weekNo < 0 || weekNo > maxWeekNo) continue;
                    double left = dayWidth, width = dayWidth * 7; //defaults for intermediate rows 
                    if (weekNo == weekNo0) //block for first/only row
                    {
                        left = Math.Max(dayWidth, dayWidth * (1 + dow0));
                        double right = (weekNo == weekNo1) ? dayWidth * (1 + dow1) : dayWidth * 8;
                        width = Math.Min(width, right - left);
                    }
                    else if (weekNo == weekNo1) //block only used for last row when there are multiple rows
                    {
                        left = dayWidth;
                        width = dayWidth * dow1;
                    }
                    bars.Add(new CalendarVM.BarVM
                    {
                        RowId = box.Box.RowId,
                        Background = UIGlobals.HIGHLIGHT_COLORS[box.HighlightColor],
                        Text = box.Box.Title,
                        Height = BARHEIGHT,
                        Top = (WEEKHEIGHT * weekNo) + (BARHEIGHT * box.HighlightColor),
                        Left = (int)left,
                        Width = (int)width
                    });
                }
            }
            VM.Bars.AddRange(bars);
        }
    }
}
