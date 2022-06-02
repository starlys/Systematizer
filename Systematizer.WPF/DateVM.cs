using System;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class DateVM : EditableVM
    {
        string _pristineDate; //set 1st time _date is set, then stays the same so that all new calculations are based on this

        string _date;
        /// <summary>
        /// In YYYYMMDD format or null; you can set it with time and it will ignore the time
        /// </summary>
        public string Date
        {
            get => _date;
            set
            {
                string d = value;
                if (d == null || d.Length < 8)
                {
                    _date = null;
                    CalendarDate = null;
                }
                else
                {
                    if (d.Length > 8) d = d[..8];
                    _date = d;
                    CalendarDate = DateUtil.ToDateTime(d);
                    if (_pristineDate == null) _pristineDate = _date;
                }
                NotifyChanged();
                RecalcDisplay();
            }
        }

        DateTime? _calendarDate;
        public DateTime? CalendarDate
        {
            get => _calendarDate;
            set { _calendarDate = value; NotifyChanged(); }
        }

        bool _isCalendarOpen;
        public bool IsCalendarOpen
        {
            get => _isCalendarOpen;
            set { _isCalendarOpen = value; NotifyChanged(); }
        }

        string _instaChange = "";
        /// <summary>
        /// One, two or three space/punctuation delimited numbers, or a day of week char (SMTWHFA)
        /// </summary>
        public string InstaChange
        {
            get => _instaChange;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _instaChange = "";
                else if (char.IsLetter(value[0]))
                {
                    //handle day of week advance
                    string newDate = DateUtil.AdvanceByShortcutKey(_pristineDate ?? _date, value[0]);
                    if (newDate != null) Date = newDate;
                    _instaChange = "";
                }
                else
                {
                    //interpret numbers as Y/M/D
                    _instaChange = value;
                    string newDate = DateUtil.ParseDateEntry(_pristineDate ?? _date, value); 
                    if (newDate != null) Date = newDate;
                }

                NotifyChanged();
            }
        }

        public string DateDisplay { get; private set; }

        void RecalcDisplay()
        {
            if (string.IsNullOrEmpty(_date))
            {
                DateDisplay = "";
                return;
            }
            DateDisplay = DateUtil.ToReadableDate(_date, includeDOW: true);
            NotifyChanged(nameof(DateDisplay));
        }
    }
}
