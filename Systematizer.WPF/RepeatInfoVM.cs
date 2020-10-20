using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class RepeatInfoVM : EditableVM
    {
        /// <summary>
        /// Note: designed so that the VM is always rebuilt and replaced rather than edited in place
        /// </summary>
        public class EntryVM : BaseVM
        {
            public EntryVM(ParsedRepeatInfo.RepeatEntry e)
            {
                Entry = e;
            }

            ParsedRepeatInfo.RepeatEntry _entry;
            public ParsedRepeatInfo.RepeatEntry Entry
            {
                get => _entry;
                private set
                {
                    _entry = value;
                    NotifyChanged("Description");
                }
            }

            public Visibility EditVisibility => ToVisibility(Entry.Kind == ParsedRepeatInfo.RepeatKind.DayOfMonth 
                || Entry.Kind == ParsedRepeatInfo.RepeatKind.NDays || Entry.Kind == ParsedRepeatInfo.RepeatKind.WeekOfMonth);

            public string Description
            {
                get
                {
                    if (_entry.Kind == ParsedRepeatInfo.RepeatKind.NDays)
                        return $"Repeat every {_entry.Arg1} days";
                    if (_entry.Kind == ParsedRepeatInfo.RepeatKind.DayOfMonth)
                        return $"Repeat on day {_entry.Arg1} of every month";
                    if (_entry.Kind == ParsedRepeatInfo.RepeatKind.WeekOfMonth)
                        return $"Repeat every {Arg2ToText(_entry.Arg2)} {DowToText(_entry.Arg1)} of the month";
                    if (_entry.Kind == ParsedRepeatInfo.RepeatKind.AddSpecific)
                        return $"Exception: also on {DateUtil.ToReadableDate(_entry.Date)}";
                    if (_entry.Kind == ParsedRepeatInfo.RepeatKind.DeleteSpecific)
                        return $"Exception: not on {DateUtil.ToReadableDate(_entry.Date)}";
                    return "";
                }
            }

            string Arg2ToText(bool[] arg2)
            {
                if (arg2 == null || arg2.Length != 5) return "";
                var words = new List<string>();
                if (arg2[0]) words.Add("first");
                if (arg2[1]) words.Add("second");
                if (arg2[2]) words.Add("third");
                if (arg2[3]) words.Add("fourth");
                if (arg2[4]) words.Add("fifth");
                return string.Join(",", words);
            }

            string DowToText(int dow)
            {
                return ((DayOfWeek)dow).ToString();
            }
        }

        public readonly ExtBox Ebox;

        public RepeatInfoVM(ExtBox ebox)
        {
            Ebox = ebox;
            var ri = ebox.Repeats;
            if (ri != null)
            {
                foreach (var entry in ri.Entries)
                    Entries.Add(new EntryVM(entry));
                HasEndDate = !ri.AutoExtend;
                EndDate.Date = ri.EndTime;
                EndTime.Time = ri.EndTime;
            }

            Entries.CollectionChanged += (s, e) =>
            {
                NotifyChanged("NoEntriesTextVisibility");
                NotifyChanged("CondensedEntriesDescription");
                NotifyChanged("AddExceptionVisibility");
                IsDirty = true;
            };
        }

        protected override void EditModeChanged()
        {
            EndDate.IsEditMode = IsEditMode;
            EndTime.IsEditMode = IsEditMode;
            NotifyChanged("ReadOnlyVisibility");
            NotifyChanged("AddExceptionVisibility");
            NotifyChanged("EndDateVisibility");
        }

        /// <summary>
        /// Convert VM to new instance of ParsedRepeatInfo, or null if there are no repeats defined
        /// </summary>
        public ParsedRepeatInfo ToParsedRepeatInfo()
        {
            if (!Entries.Any()) return null;

            var p = new ParsedRepeatInfo
            {
                AutoExtend = !HasEndDate,
                EndTime = EndDate.Date + EndTime.Time,
                Entries = Entries.Select(x => x.Entry).ToList()
            };
            if (!HasEndDate) p.EndTime = DateUtil.ToYMDHM(DateTime.Today.AddYears(1));
            return p;
        }

        public ObservableCollection<EntryVM> Entries { get; set; } = new ObservableCollection<EntryVM>();

        public Visibility NoEntriesTextVisibility => ToVisibility(!Entries.Any());
        public Visibility EndDateVisibility => ToVisibility(IsEditMode && HasEndDate);

        public string CondensedEntriesDescription => string.Join("; ", Entries.Select(x => x.Description));

        /// <summary>
        /// Visibility binding (visible when read only)
        /// </summary>
        public Visibility ReadOnlyVisibility => ToVisibility(!IsEditMode);

        public Visibility AddExceptionVisibility => ToVisibility(IsEditMode && Entries.Any());

        bool _hasEndDate;
        public bool HasEndDate
        {
            get => _hasEndDate;
            set
            {
                _hasEndDate = value;
                NotifyChanged();
                NotifyChanged("EndDateVisibility");
            }
        }

        public DateVM EndDate { get; set; } = new DateVM() { Date = DateTime.Today.Year + "1231" };
        public TimeVM EndTime { get; set; } = new TimeVM() { Time = "2359" };
    }
}
