using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for RepeatExceptionDialog.xaml
    /// </summary>
    public partial class RepeatExceptionDialog : Window
    {
        class ProjectedTime
        {
            public string Time { get; set; } //in YYYYMMDDHHMM
            public string ReadableTime { get; set; }
            public override string ToString() => ReadableTime;
        }

        class VM : BaseVM
        {
            public RepeatExceptionDialog Owner;

            bool _isAdd;
            public bool IsAdd
            {
                get => _isAdd;
                set
                {
                    _isAdd = true;
                    _isDelete = false;
                    _isAddDelete = false;
                    NotifyRadioButtons();
                }
            }

            bool _isDelete;
            public bool IsDelete
            {
                get => _isDelete;
                set
                {
                    _isDelete = true;
                    _isAdd = false;
                    _isAddDelete = false;
                    NotifyRadioButtons();
                }
            }

            bool _isAddDelete = true;
            public bool IsAddDelete
            {
                get => _isAddDelete;
                set
                {
                    _isAddDelete = true;
                    _isDelete = false;
                    _isAdd = false;
                    NotifyRadioButtons();
                }
            }

            void NotifyRadioButtons()
            {
                NotifyChanged(nameof(IsAdd));
                NotifyChanged(nameof(IsDelete));
                NotifyChanged(nameof(IsAddDelete));
                NotifyChanged(nameof(AddVisibility));
                NotifyChanged(nameof(DeleteVisibility));
                NotifyChanged(nameof(IsValid));
            }

            List<ProjectedTime> _instances;
            public List<ProjectedTime> Instances
            {
                get
                {
                    if (_instances == null)
                        _instances = Owner.GetProjection();
                    return _instances;
                }
            }

            int _selectedIdx = -1; 
            public int SelectedIdx
            {
                get => _selectedIdx;
                set
                {
                    _selectedIdx = value;
                    NotifyChanged();
                    NotifyChanged(nameof(IsValid));
                }
            }

            public Visibility AddVisibility => ToVisibility(IsAdd || IsAddDelete);
            public Visibility DeleteVisibility => ToVisibility(IsDelete || IsAddDelete);

            public DateVM AddedDate { get; set; } = new DateVM();
            public TimeVM AddedTime { get; set; } = new TimeVM();

            public bool IsValid
            {
                get
                {
                    if ((IsDelete || IsAddDelete) && SelectedIdx < 0) return false;
                    return true;
                }
            }
        }

        RepeatInfoVM CallerVM;

        public RepeatExceptionDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Allow user to make an exception, and return 0, 1, or 2 entries to add to the caller's VM
        /// </summary>
        internal static List<ParsedRepeatInfo.RepeatEntry> MakeException(RepeatInfoVM callerVM)
        {
            var ret = new List<ParsedRepeatInfo.RepeatEntry>(2);

            //guess the most likely time of an exception - the first other repeat pattern defined with a time
            string defaultTime = "1200";
            foreach (var otherPat in callerVM.Entries)
                if (otherPat.Entry.Time != null && otherPat.Entry.Time.Length == 4)
                {
                    defaultTime = otherPat.Entry.Time;
                    break;
                }

            //prep VM and show dialog
            var dialog = new RepeatExceptionDialog
            {
                Owner = App.Current.MainWindow,
                CallerVM = callerVM
            };
            var vm = new VM { Owner = dialog };
            vm.AddedDate.Date = DateUtil.ToYMDHM(DateTime.Today);
            vm.AddedDate.IsEditMode = true;
            vm.AddedTime.Time = defaultTime; 
            vm.AddedTime.IsEditMode = true;
            dialog.DataContext = vm;
            if (dialog.ShowDialog() != true) return ret;

            //record the exceptions
            if (vm.IsDelete || vm.IsAddDelete)
            {
                var dt = vm.Instances[vm.SelectedIdx].Time;
                ret.Add(new ParsedRepeatInfo.RepeatEntry
                {
                    Kind = ParsedRepeatInfo.RepeatKind.DeleteSpecific,
                    Date = dt.Substring(0, 8),
                    Time = dt.Substring(8, 4)
                });
            }
            if (vm.IsAdd || vm.IsAddDelete)
            {
                ret.Add(new ParsedRepeatInfo.RepeatEntry
                {
                    Kind = ParsedRepeatInfo.RepeatKind.AddSpecific,
                    Date = vm.AddedDate.Date,
                    Time = vm.AddedTime.Time
                });
            }
            return ret;
        }

        List<ProjectedTime> GetProjection()
        {
            //repeatProjecotr requires cachedbox, so make a fake one
            var projector = new RepeatProjector();
            var ri = CallerVM.ToParsedRepeatInfo();
            var cbox = new CachedBox { BoxTime = CallerVM.Ebox.Box.BoxTime, Repeats = ri };
            var agendaEntries = projector.Project(cbox, false, false);
            return agendaEntries
                .OrderBy(r => r.Time)
                .Select(r => new ProjectedTime
                {
                    Time = r.Time,
                    ReadableTime = DateUtil.ToReadableDate(r.Time) + " at " + DateUtil.ToReadableTime(r.Time)
                })
                .ToList();
        }

        void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
