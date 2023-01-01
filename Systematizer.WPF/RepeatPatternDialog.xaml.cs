using System.Collections.ObjectModel;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for RepeatPatternDialog.xaml
/// </summary>
public partial class RepeatPatternDialog : Window
{
    class DayOfWeekVM : BaseVM
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }

    class WeekOfMonthVM : BaseVM
    {
        public PatternVM Parent;

        bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyChanged();
                Parent.WeekOfMonthChanged();
            }
        }

        string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyChanged();
            }
        }
    }

    class PatternVM : BaseVM
    {
        bool _kindIsNDays = true;
        public bool KindIsNDays
        {
            get => _kindIsNDays;
            set
            {
                _kindIsNDays = value;
                NotifyChanged();
                if (value)
                {
                    KindIsDayOfMonth = false;
                    KindIsWeekOfMonth = false;
                }
            }
        }

        bool _kindIsDayOfMonth;
        public bool KindIsDayOfMonth
        {
            get => _kindIsDayOfMonth;
            set
            {
                _kindIsDayOfMonth = value;
                NotifyChanged();
                if (value)
                {
                    KindIsNDays = false;
                    KindIsWeekOfMonth = false;
                }
            }
        }

        bool _kindIsWeekOfMonth;
        public bool KindIsWeekOfMonth
        {
            get => _kindIsWeekOfMonth;
            set
            {
                _kindIsWeekOfMonth = value;
                NotifyChanged();
                if (value)
                {
                    KindIsNDays = false;
                    KindIsDayOfMonth = false;
                }
            }
        }

        public TimeVM Time { get; set; } = new TimeVM();

        int _nDaysArg = 1;
        public int NDaysArg
        {
            get => _nDaysArg;
            set
            {
                _nDaysArg = value;
                NotifyChanged();
            }
        }

        int _dayOfMonthArg = 1;
        public int DayOfMonthArg
        {
            get => _dayOfMonthArg;
            set
            {
                _dayOfMonthArg = value;
                NotifyChanged();
            }
        }

        int _weekOfMonthArg1 = 0;
        public int WeekOfMonthArg1 //day of week
        {
            get => _weekOfMonthArg1;
            set
            {
                _weekOfMonthArg1 = value;
                NotifyChanged();
            }
        }

        public string WeeksOfMonthDisplay => string.Join(", ", WeeksOfMonth.Where(w => w.IsSelected).Select(w => w.Name));

        public ObservableCollection<DayOfWeekVM> DaysOfWeek { get; set; } = new ObservableCollection<DayOfWeekVM>();
        public ObservableCollection<WeekOfMonthVM> WeeksOfMonth { get; set; } = new ObservableCollection<WeekOfMonthVM>();

        public void WeekOfMonthChanged()
        {
            NotifyChanged(nameof(WeeksOfMonthDisplay));
        }
    }

    PatternVM VM;
    ParsedRepeatInfo.RepeatEntry Result;

    public RepeatPatternDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Allow user entry or modification of argument, returning newly constructed entry or null if canceled
    /// </summary>
    public static ParsedRepeatInfo.RepeatEntry ShowDialog(ParsedRepeatInfo.RepeatEntry e)
    {
        //load VM from pattern entry 
        var vm = new PatternVM();
        vm.Time.Time = e.Time;
        vm.Time.IsEditMode = true;
        vm.WeeksOfMonth.Add(new WeekOfMonthVM { Parent = vm, Name = "First" });
        vm.WeeksOfMonth.Add(new WeekOfMonthVM { Parent = vm, Name = "Second" });
        vm.WeeksOfMonth.Add(new WeekOfMonthVM { Parent = vm, Name = "Third" });
        vm.WeeksOfMonth.Add(new WeekOfMonthVM { Parent = vm, Name = "Fourth" });
        vm.WeeksOfMonth.Add(new WeekOfMonthVM { Parent = vm, Name = "Fifth" });
        for(int dow = 0; dow < 7; ++dow)
            vm.DaysOfWeek.Add(new DayOfWeekVM { Value = dow, Name = ((DayOfWeek)dow).ToString() });

        if (e.Kind == ParsedRepeatInfo.RepeatKind.NDays)
        {
            vm.NDaysArg = e.Arg1;
        }
        else if (e.Kind == ParsedRepeatInfo.RepeatKind.DayOfMonth)
        {
            vm.KindIsDayOfMonth = true;
            vm.DayOfMonthArg = e.Arg1;
        }
        else if (e.Kind == ParsedRepeatInfo.RepeatKind.WeekOfMonth)
        {
            vm.KindIsWeekOfMonth = true;
            vm.WeekOfMonthArg1 = e.Arg1;
            if (e.Arg2 != null && e.Arg2.Length == 5)
                for (int w = 0; w < 5; ++w)
                    if (e.Arg2[w])
                        vm.WeeksOfMonth[w].IsSelected = true;
        }
        else throw new Exception("Call error: dialog only supports 3 kinds");

        //show dialog
        var dlg = new RepeatPatternDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = vm,
            VM = vm
        };
        if (dlg.ShowDialog() != true) return null;
        return dlg.Result;
    }

    void OK_Click(object sender, RoutedEventArgs e)
    {
        VisualUtils.LoseRegainFocus();
        Result = Validate();
        if (Result != null) DialogResult = true;
    }

    /// <summary>
    /// Get valid parsed dialog result; or if error, show error and return null
    /// </summary>
    ParsedRepeatInfo.RepeatEntry Validate()
    {
        string message = null;
        var ret = new ParsedRepeatInfo.RepeatEntry
        {
            Time = VM.Time.Time
        };
        if (VM.KindIsNDays)
        {
            ret.Kind = ParsedRepeatInfo.RepeatKind.NDays;
            ret.Arg1 = VM.NDaysArg;
            if (ret.Arg1 < 1) message = "Must be a positive number of days";
        }
        else if (VM.KindIsDayOfMonth)
        {
            ret.Kind = ParsedRepeatInfo.RepeatKind.DayOfMonth;
            ret.Arg1 = VM.DayOfMonthArg;
            if (ret.Arg1 < 1 || ret.Arg1 > 28) message = "Must be day 1 to 28";
        }
        else
        {
            ret.Kind = ParsedRepeatInfo.RepeatKind.WeekOfMonth;
            ret.Arg1 = VM.WeekOfMonthArg1;
            if (ret.Arg1 < 0 || ret.Arg1 > 6) message = "Must choose day of week";
            ret.Arg2 = VM.WeeksOfMonth.Select(v => v.IsSelected).ToArray();
            if (ret.Arg2.All(b => !b)) message = "At least one week of the month must be selected";
        }

        if (message != null)
        {
            VisualUtils.ShowMessageDialog(message);
            return null;
        }
        return ret;
    }

    private void NDaysDetail_GotFocus(object sender, RoutedEventArgs e)
    {
        VM.KindIsNDays = true;
    }

    private void DayOfMonthDetail_GotFocus(object sender, RoutedEventArgs e)
    {
        VM.KindIsDayOfMonth = true;
    }

    private void WeekOfMonthDetail_GotFocus(object sender, RoutedEventArgs e)
    {
        VM.KindIsWeekOfMonth = true;
    }
}
