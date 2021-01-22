using System;
using System.Collections.Generic;
using System.Text;

namespace Systematizer.Common
{
    /// <summary>
    /// Box.RepeatInfo in parsed form
    /// </summary>
    public class ParsedRepeatInfo
    {
        public enum RepeatKind { NDays, WeekOfMonth, DayOfMonth, AddSpecific, DeleteSpecific }

        public class RepeatEntry
        {
            public RepeatKind Kind;
            public string Date; //only used for AddSpecific, DeleteSpecific; in YYYYMMDD format
            public string Time = "0900"; //in HHMM format
            public int Arg1 = 1; //for NDays, the number of days between; for DayOfMonth, the day of the month; for WeekOfMonth, the day of week (0=sunday)
            public bool[] Arg2; //for WeekOfMonth, an array of 5 weeks; else null
        }

        /// <summary>
        /// YYYYMMDDHHMM format end date, guaranteed to be at least a year if the autoextend flag is set
        /// </summary>
        public string EndTime;

        public bool AutoExtend = true;

        public List<RepeatEntry> Entries = new List<RepeatEntry>();

        /// <summary>
        /// create instance if this has nonnull RepeatInfo, or null
        /// </summary>
        internal static ParsedRepeatInfo Build(string packedValue)
        {
            //not rpeating
            if (string.IsNullOrEmpty(packedValue)) return null;

            //classify the segments
            var parsed = new ParsedRepeatInfo();
            parsed = new ParsedRepeatInfo();
            var segments = packedValue.Split('|', StringSplitOptions.RemoveEmptyEntries);
            string maxEndTime = DateUtil.ToYMDHM(DateTime.Today.AddYears(1));
            parsed.EndTime = maxEndTime;
            parsed.AutoExtend = false;
            foreach (string segment in segments)
            {
                if (segment.Length < 1) continue;
                char meaning = segment[0];
                if (meaning == 'e')
                {
                    if (segment.Length == 14)
                        parsed.EndTime = segment[2..];
                }
                else if (meaning == 'x')
                {
                    parsed.AutoExtend = true;
                }
                else if (meaning == 'p')
                {
                    if (segment.Length > 6)
                    {
                        if (segment[1] == 'd')
                            parsed.Entries.Add(new RepeatEntry
                            {
                                Kind = RepeatKind.NDays,
                                Time = segment.Substring(3, 4),
                                Arg1 = DateUtil.ParseInt(segment[6..], 1)
                            });
                        else if (segment[1] == 'w' && segment.Length > 11)
                            parsed.Entries.Add(new RepeatEntry
                            {
                                Kind = RepeatKind.WeekOfMonth,
                                Time = segment.Substring(3, 4),
                                Arg2 = new[] { segment[7] == 'Y', segment[8] == 'Y', segment[9] == 'Y', segment[10] == 'Y', segment[11] == 'Y' },
                                Arg1 = DateUtil.ParseInt(segment[12..], 1)
                            });
                        if (segment[1] == 'm')
                            parsed.Entries.Add(new RepeatEntry
                            {
                                Kind = RepeatKind.DayOfMonth,
                                Time = segment.Substring(3, 4),
                                Arg1 = DateUtil.ParseInt(segment[7..], 1)
                            });
                    }
                }
                else if (meaning == 'a')
                {
                    if (segment.Length == 14)
                    {
                        parsed.Entries.Add(new RepeatEntry
                        {
                            Kind = RepeatKind.AddSpecific,
                            Date = segment.Substring(2, 8),
                            Time = segment.Substring(10, 4)
                        });
                    }
                }
                else if (meaning == 'd')
                {
                    if (segment.Length == 14)
                    {
                        parsed.Entries.Add(new RepeatEntry
                        {
                            Kind = RepeatKind.DeleteSpecific,
                            Date = segment.Substring(2, 8),
                            Time = segment.Substring(10, 4)
                        });
                    }
                }
            }

            //push out end time if autoextending
            if (parsed.AutoExtend && DateUtil.IsBefore(parsed.EndTime, maxEndTime))
                parsed.EndTime = maxEndTime;

            return parsed;
        }

        /// <summary>
        /// Pack the contents of this container into database storage for Box.RepeatInfo column
        /// </summary>
        public string PackForStorage()
        {
            var segments = new List<string>
            {
                $"e={EndTime}"
            };
            if (AutoExtend) segments.Add("x");
            foreach (var entry in Entries)
            {
                if (entry.Kind == RepeatKind.AddSpecific)
                    segments.Add($"a={entry.Date}{entry.Time}");
                else if (entry.Kind == RepeatKind.DeleteSpecific)
                    segments.Add($"d={entry.Date}{entry.Time}");
                else if (entry.Kind == RepeatKind.NDays)
                    segments.Add($"pd={entry.Time}{entry.Arg1}");
                else if (entry.Kind == RepeatKind.WeekOfMonth)
                    segments.Add($"pw={entry.Time}{PackArg2ForStorage(entry.Arg2)}{entry.Arg1}");
                else if (entry.Kind == RepeatKind.DayOfMonth)
                    segments.Add($"pm={entry.Time}{entry.Arg1}");
            }
            return string.Join('|', segments);
        }

        /// <summary>
        /// pack an array of bools into exactly 5 Y or N chars, like "NNYNN"
        /// </summary>
        string PackArg2ForStorage(bool[] a)
        {
            var ret = new StringBuilder(10);
            if (a != null)
                foreach (bool b in a) ret.Append(b ? 'Y' : 'N');
            ret.Append("NNNNN"); //in case array missing or wrong length
            ret.Length = 5;
            return ret.ToString();
        }
    }
}
