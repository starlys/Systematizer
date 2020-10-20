using System;
using System.Linq;

namespace Systematizer.Common
{
    /// <summary>
    /// Helpers for date/times
    /// </summary>
    public static class DateUtil
    {
        public static readonly char[] ALLDELIMITERS = new[] { ' ', '.', '/', '`', '-', ':' };
        public const int EARLY_YEAR = 1970;
        public const string DEFAULT_TIME = "0000";

        public static string ToYMD(DateTime d) => d.ToString("yyyyMMdd");
        
        public static string ToYMDHM(DateTime d) => d.ToString("yyyyMMddHHmm");

        public static string ToReadableDate(DateTime d) => d.ToString("yyyy-M-d");

        /// <summary>
        /// Convert YYYYMMDD to readable
        /// </summary>
        public static string ToReadableDate(string d, bool includeDOW = false)
        {
            if (d == null || d.Length < 8) return "";
            string ret = $"{d.Substring(0, 4)}-{d.Substring(4, 2)}-{d.Substring(6, 2)}";
            if (includeDOW) ret += " - " + ToDateTime(d).Value.ToString("ddd");
            return ret;
        }

        public static string ToReadableTime(DateTime d) => d.ToString("HH:mm");

        /// <summary>
        /// Convert YYYYMMDDHHMM or HHMM to readable time
        /// </summary>
        public static string ToReadableTime(string dt)
        {
            if (dt == null) return "";
            if (dt.Length == 12) dt = dt.Substring(8, 4);
            else if (dt.Length != 4) return "";
            return $"{dt.Substring(0, 2)}:{dt.Substring(2, 2)}";
        }

        /// <summary>
        /// Convert a string in YYYYMMDD or YYYYMMDDHHMM format to a local datetime
        /// </summary>
        /// <returns>null if unparsable</returns>
        public static DateTime? ToDateTime(string s)
        {
            if (s == null || s.Length < 8) return null;
            int yr = ParseInt(s.Substring(0, 4), -1);
            int mo = ParseInt(s.Substring(4, 2), -1);
            int dy = ParseInt(s.Substring(6, 2), -1);
            int hr = 0, mi = 0;
            if (s.Length == 12)
            {
                hr = ParseInt(s.Substring(8, 2), -1);
                mi = ParseInt(s.Substring(10, 2), -1);
            }
            if (yr < 0 || mo < 0 || dy < 0 || hr < 0 || mi < 0) return null;
            return new DateTime(yr, mo, dy, hr, mi, 0);
        }

        /// <summary>
        /// parse int and return default if not possible
        /// </summary>
        public static int ParseInt(string s, int defaultValue)
        {
            if (int.TryParse(s, out int n)) return n;
            return defaultValue;
        }

        /// <summary>
        /// Shortcut for comparing dates in YYMMDD... format (just uses string compare)
        /// </summary>
        /// <returns>true if d1 is before d2</returns>
        public static bool IsBefore(string d1, string d2)
        {
            return string.CompareOrdinal(d1, d2) < 0;
        }

        /// <summary>
        /// Parse durations in the form Nu where N is a number and u (unit) is m, h, d
        /// </summary>
        /// <returns>null if empty or unparsable</returns>
        public static TimeSpan? ParseDuration(string dur)
        {
            if (dur == null || dur.Length < 2) return null;
            char unit  = dur[dur.Length - 1];
            if (!int.TryParse(dur.Substring(0, dur.Length - 1), out int num)) return null;
            if (unit == 'm') return TimeSpan.FromMinutes(num);
            if (unit == 'h') return TimeSpan.FromHours(num);
            if (unit == 'd') return TimeSpan.FromDays(num);
            return null;
        }

        /// <summary>
        /// Given an optional starting date (uses today if missing) and a day of the week or
        /// a number of days (1-9), return the
        /// date that is the next occuring one on that day of the week or plus the designated number
        /// of days. Uses YYYYMMDD format.
        /// </summary>
        /// <returns>null if bad shortcut</returns>
        public static string AdvanceByShortcutKey(string fromDate, char shortcut)
        {
            var d = ToDateTime(fromDate) ?? DateTime.Today;

            //is digit?
            if (shortcut >= '1' && shortcut <= '9')
            {
                d = d.AddDays((int)shortcut - (int)'0');
            }
            
            //is DOW?
            else
            {
                char c = char.ToUpperInvariant(shortcut);
                int targetDow = Array.IndexOf(Constants.DAYOFWEEK_CHARS, c);
                if (targetDow < 0) return null;
                int actualDow = (int)d.DayOfWeek;
                if (targetDow > actualDow)
                    d = d.AddDays(targetDow - actualDow);
                else
                    d = d.AddDays(targetDow - actualDow + 7);
            }

            return ToYMD(d);
        }

        /// <summary>
        /// Given a user entry of a time, return it as HHMM format. If unparsable, returns "0000".
        /// </summary>
        public static string ParseTimeEntry(string entry)
        {
            if (entry == null || entry.Length == 0) return DEFAULT_TIME;

            //get entries as ints or fail
            var partsS = entry.Split(ALLDELIMITERS, StringSplitOptions.RemoveEmptyEntries);
            int[] partsN;
            try { partsN = partsS.Select(s => int.Parse(s)).ToArray(); }
            catch { return DEFAULT_TIME; }
            if (partsN.Length == 0) return DEFAULT_TIME;

            //if 1 entry, it could be hour only (0-23) or hour and minute (000-059, 100-159, ...2300-2359)
            if (partsN.Length == 1)
            {
                string hourAndMinute = partsS[0];
                if (hourAndMinute.Length == 1)
                {
                    //hour 0..9
                    return $"0{hourAndMinute}00";
                }
                if (hourAndMinute.Length == 2)
                {
                    //hour 10..23 (or 00..09)
                    if (partsN[0] > 23) return DEFAULT_TIME;
                    return $"{hourAndMinute}00";
                }
                if (hourAndMinute.Length == 3)
                {
                    //hour 0..9 and minute 00..59
                    int h = int.Parse(hourAndMinute.Substring(0, 1));
                    int m = int.Parse(hourAndMinute.Substring(1, 2));
                    if (m > 59) return DEFAULT_TIME;
                    return $"0{hourAndMinute}";
                }
                if (hourAndMinute.Length == 4)
                {
                    //hour 10..23 and minute 00.59 (or hour 00..09)
                    int h = int.Parse(hourAndMinute.Substring(0, 2));
                    int m = int.Parse(hourAndMinute.Substring(2, 2));
                    if (h > 23 || m > 59) return DEFAULT_TIME;
                    return hourAndMinute;
                }
                return DEFAULT_TIME;
            }

            //if 2+ entries, hour and minute are separate
            {
                int h = partsN[0];
                int m = partsN[1];
                if (h > 23 || m > 59) return DEFAULT_TIME;
                return $"{h:D2}{m:D2}";
            }
        }

        /// <summary>
        /// Add a duration in "4d"/"15m" style to a time in YYYYMMDDHHMM format, returning the later time
        /// </summary>
        public static string AddDuration(string time, string duration)
        {
            var dt = ToDateTime(time);
            if (dt == null) return null;
            return ToYMDHM(AddDuration(dt.Value, duration));
        }

        /// <summary>
        /// Add a duration in "4d"/"15m" style to a DateTime, returning the later DateTime
        /// </summary>
        public static DateTime AddDuration(DateTime dt, string duration)
        {
            var delta = ParseDuration(duration);
            if (delta != null) dt = dt.Add(delta.Value);
            return dt;
        }

        /// <summary>
        /// Given an optional starting date (uses today if missing) and a user entry of numbers and separators,
        /// return a rational date. Uses YYYYMMDD format
        /// </summary>
        /// <returns>fromDate if entry is unparsable</returns>
        public static string ParseDateEntry(string fromDate, string entry)
        {
            //get entries as ints
            if (entry == null) return fromDate;
            var parts = entry.Split(ALLDELIMITERS, StringSplitOptions.RemoveEmptyEntries);
            int[] entries;
            try { entries = parts.Select(s => int.Parse(s)).ToArray(); }
            catch { return fromDate; }

            //get starting y/m/d
            var fromDate2 = ToDateTime(fromDate) ?? DateTime.Today;
            int y0 = fromDate2.Year, m0 = fromDate2.Month, d0 = fromDate2.Day;

            //apportion the entries to new y/m/d depending on the range and number of entries
            int y1 = 0, m1 = 0, d1 = 0;
            if (entries.Length == 1)
            {
                d1 = entries[0];
                if (d1 > EARLY_YEAR) { y1 = d1; d1 = 0; } //allow single entry like 2018 to indicate year
            }
            else if (entries.Length == 2)
            {
                m1 = entries[0];
                d1 = entries[1];
            }
            else if (entries.Length >= 3)
            {
                y1 = entries[0];
                if (y1 < 100) y1 += 2000;
                m1 = entries[1];
                d1 = entries[2];
            }
            if (
                d1 > 31 
                || m1 > 12 
                || (y1 > 0 && (y1 < EARLY_YEAR || y1 > 2999))
                )
                return fromDate;

            //fill in missing y/m/d from starting values
            DateTime targetDate;
            if (y1 > 0)
            {
                //year specified (possibly by itself)
                if (m1 == 0) m1 = m0;
                if (d1 == 0) d1 = d0;
                targetDate = new DateTime(y1, m1, d1);
            }
            else if (m1 > 0)
            {
                //year missing but month specified
                if (d1 == 0) d1 = d0;
                targetDate = new DateTime(y0, m1, d1);
                if (targetDate < fromDate2) targetDate = targetDate.AddYears(1);
            }
            else if (d1 > 0)
            {
                //only day specified
                targetDate = new DateTime(y0, m0, d1);
                if (targetDate < fromDate2) targetDate = targetDate.AddMonths(1);
            }
            else return fromDate;

            return ToYMD(targetDate);
        }

        /// <summary>
        /// In YYYYMMDDHHMM format, find the time half way between the two given times;
        /// or null on unparsable error
        /// </summary>
        public static string HalfWayBetween(string t1, string t2)
        {
            DateTime? dt1 = ToDateTime(t1), dt2 = ToDateTime(t2);
            if (!dt1.HasValue || !dt2.HasValue) return null;
            var diff = dt2.Value.Subtract(dt1.Value); //can be negative
            DateTime dtHalf = dt1.Value.AddTicks(diff.Ticks / 2);
            return ToYMDHM(dtHalf);
        }
    }
}
