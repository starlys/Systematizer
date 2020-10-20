using System;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class TimeVM : EditableVM
    {
        bool TimeChanging;

        string _time;
        /// <summary>
        /// Time in HHMM format; can also set to YYYYMMDDHHMM format and it will ignore the date
        /// </summary>
        public string Time
        {
            get => _time;
            set
            {
                if (value == null || value.Length < 4)
                    _time = null;
                else if (value.Length == 12)
                    _time = value.Substring(8, 4);
                else
                    _time = value;
                NotifyChanged();
                TimeChanging = true;
                DisplayTime = DateUtil.ToReadableTime(_time);
                TimeChanging = false;
            }
        }

        string _displayTime;
        public string DisplayTime
        {
            get => _displayTime;
            set
            {
                _displayTime = value;
                NotifyChanged();
                if (!TimeChanging) Time = DateUtil.ParseTimeEntry(value);
            }
        }
    }
}
