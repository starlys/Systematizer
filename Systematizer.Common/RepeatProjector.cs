using System;
using System.Collections.Generic;
using System.Linq;

namespace Systematizer.Common
{
    /// <summary>
    /// Behavior to create repeated agenda entries for repeated tasks
    /// </summary>
    public class RepeatProjector
    {
        /// <summary>
        /// Given a box with repeats, find the next time it should be scheduled for. Use this for processing DONE command.
        /// </summary>
        /// <returns>YYYYMMDDHHMM, or null if there are no repeats defined, or there are no more instances available</returns>
        public string AdvanceTime(string boxTime, ParsedRepeatInfo repeats)
        {
            //simulate cached box, knowing that Project function only looks at BoxTime and Repeats members
            var simulatedBox = new CachedBox { BoxTime = boxTime, Repeats = repeats };

            var seq = Project(simulatedBox, false).OrderBy(r => r.Time);
            try
            {
                var next = seq.First();
                return next.Time;
            }
            catch
            {
                return null; //sequence empty
            }
        }

        /// <summary>
        /// With delayed execution, find all cases where a task should be repeated
        /// </summary>
        /// <param name="box">Only inspects box.BoxTime and Repeats</param>
        /// <param name="includeCurrent">if true, includes the time the box is currently scheduled for</param>
        public IEnumerable<AgendaEntry> Project(CachedBox box, bool includeCurrent)
        {
            //include the specific time it is now scheduled for whether repeating or not
            if (includeCurrent)
            {
                (DateTime? remindAt1, DateTime? remindAt2) = CalcPendingReminder(box);
                yield return new AgendaEntry
                {
                    Box = box,
                    Time = box.BoxTime,
                    PendingPrepReminder = remindAt1,
                    PendingReminder = remindAt2
                };
            }

            if (box.Repeats != null)
            {
                DateTime? max = DateUtil.ToDateTime(box.Repeats.EndTime);
                if (max == null) max = DateTime.Today;

                //convert add and delete exceptions to datetime strings and start with all the added ones
                var deleteExceptions = box.Repeats.Entries.Where(e => e.Kind == ParsedRepeatInfo.RepeatKind.DeleteSpecific)
                    .Select(d => d.Date + d.Time);
                var addExceptions = box.Repeats.Entries.Where(e => e.Kind == ParsedRepeatInfo.RepeatKind.AddSpecific) 
                    .Select(d => d.Date + d.Time);
                var allTimes = new HashSet<string>();
                foreach (string t in addExceptions) allTimes.Add(t);

                //go through all patterns and add times for those unless deleted
                foreach (var pat in box.Repeats.Entries)
                {
                    //handle add and delete
                    if (pat.Kind == ParsedRepeatInfo.RepeatKind.DeleteSpecific) continue;
                    if (pat.Kind == ParsedRepeatInfo.RepeatKind.AddSpecific)
                    {
                        allTimes.Add(pat.Date + pat.Time);
                        continue;
                    }

                    //handle all other pattern kinds
                    (int hr, int mi) = DateUtil.ToHourMinute(pat.Time, 9, 0);
                    DateTime? running = DateUtil.ToDateTime(box.BoxTime);
                    if (running == null) continue;
                    for (int iter = 0; iter < 500; ++iter) //infinite loop control
                    {
                        running = NextTime(running.Value, max.Value, pat, hr, mi);
                        if (running == null) break;
                        string t2 = DateUtil.ToYMDHM(running.Value);
                        if (deleteExceptions.Contains(t2)) continue;
                        allTimes.Add(t2);
                    }
                }

                //provide return values 
                foreach (string t in allTimes)
                {
                    yield return new AgendaEntry
                    {
                        Box = box,
                        Time = t
                    };
                }
            }
        }

        /// <summary>
        /// Advance a given time to the next time
        /// </summary>
        /// <returns>null if no more</returns>
        DateTime? NextTime(DateTime d, DateTime max, ParsedRepeatInfo.RepeatEntry r, int hour, int minute)
        {
            if (r.Kind == ParsedRepeatInfo.RepeatKind.DeleteSpecific || r.Kind == ParsedRepeatInfo.RepeatKind.AddSpecific)
                throw new Exception("Call error");

            //the following blocks advance d to the correct day or abort
            if (r.Kind == ParsedRepeatInfo.RepeatKind.NDays)
            {
                d = d.AddDays(r.Arg1);
            }
            else if (r.Kind == ParsedRepeatInfo.RepeatKind.WeekOfMonth)
            {
                //ensure arg2 has 5 values and at least 1 true value 
                if (r.Arg2 == null || r.Arg2.Length != 5 || r.Arg2.All(x => !x)) return null;

                //advance to next day that matches the requied day of week (even if not the required week of month)
                DayOfWeek dow = (DayOfWeek)r.Arg1;
                do { d = d.AddDays(1); } while (d.DayOfWeek != dow);

                //advance by weeks until we get to the right week of the month
                for (int iter = 0; iter < 200; ++iter) //infinite loop control
                {
                    int weekOfMonth = (d.Day - 1) / 7; //0..4
                    if (r.Arg2[weekOfMonth]) break;
                    d = d.AddDays(7);
                    //worst case: "every 5th saturday" may skip a few months but eventually there will be a 5th saturday
                }
            }
            else if (r.Kind == ParsedRepeatInfo.RepeatKind.DayOfMonth)
            {
                if (r.Arg1 < 1 || r.Arg1 > 31) return null;

                //advance to next day that matches the required day of month
                do { d = d.AddDays(1); } while (d.Day != r.Arg1);
            }

            //apply the exact hour/minute 
            d = new DateTime(d.Year, d.Month, d.Day, hour, minute, 0);
            if (d > max) return null;
            return d;
        }

        /// <summary>
        /// Get the prep reminder and appointment reminder times, or nulls if not used
        /// </summary>
        (DateTime?, DateTime?) CalcPendingReminder(CachedBox box)
        {
            if (box.TimeType != Constants.TIMETYPE_MINUTE) return (null, null);
            DateTime? remindAt2 = DateUtil.ToDateTime(box.BoxTime);
            if (!remindAt2.HasValue) return (null, null);
            DateTime? remindAt1 = null;
            TimeSpan? dur = DateUtil.ParseDuration(box.PrepDuration);
            if (dur != null)
            {
                remindAt1 = remindAt2.Value.Subtract(dur.Value);
                if (remindAt1.Value < DateTime.Now) remindAt1 = null;
            }
            if (remindAt2.Value < DateTime.Now) remindAt2 = null;
            return (remindAt1, remindAt2);
        }
    }
}
