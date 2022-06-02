using System;
using System.Collections.Generic;
using System.Linq;

namespace Systematizer.Common
{
    /// <summary>
    /// The parsed contents of Setting.ChunkInfo
    /// </summary>
    public class MultiDayChunkSet
    {
        public const int STARTEVENINGHR = 18, STARTAFTERNOONHR = 12;

        public class Chunk
        {
            public string Title;
            public long[] BoxIds; //or null if this is the first chunk of the day or not initialized
        }

        public class DayChunkSet
        {
            public string Date; //YYYYMMDD
            public List<Chunk> Chunks = new List<Chunk>(); //first one is catch-all and does not have box ids
        }

        public List<DayChunkSet> Days = new List<DayChunkSet>(2);

        /// <summary>
        /// Initialize from database value (packed)
        /// </summary>
        public void Initialize(string chunkInfo)
        {
            Days.Clear();
            var segments = chunkInfo.Split('|', StringSplitOptions.RemoveEmptyEntries);
            DayChunkSet dcs = null;
            foreach (string segment in segments)
            {
                if (segment.Length < 9) continue;
                string date = segment[..8];
                string[] idsAndTitle = segment[8..].Split(new char[] { ';' }, 2);
                if (idsAndTitle.Length != 2) continue;

                var boxIds = new List<long>();
                foreach (string idS in idsAndTitle[0].Split(','))
                {
                    if (long.TryParse(idS, out long id)) boxIds.Add(id);
                }

                bool isFirstChunkOnDate = dcs == null || dcs.Date != date;
                var c = new Chunk
                {
                    Title = idsAndTitle[1],
                    BoxIds = isFirstChunkOnDate ? null : boxIds.ToArray()
                };

                //add dcs to Days for the current date
                if (isFirstChunkOnDate)
                {
                    dcs = new DayChunkSet { Date = date };
                    Days.Add(dcs);
                }

                dcs.Chunks.Add(c);
            }
        }

        /// <summary>
        /// Throw out old days and ensure today and tomorrow are initialized
        /// </summary>
        /// <returns>true if any changes made</returns>
        public bool ResetForToday(IEnumerable<CachedBox> scheduledBoxes)
        {
            string today = DateUtil.ToYMD(DateTime.Today);
            string tomorrow = DateUtil.ToYMD(DateTime.Today.AddDays(1));

            //throw away old
            while (Days.Count > 0 && DateUtil.IsBefore(Days[0].Date, today))
                Days.RemoveAt(0);

            //this should not happen but if db is bad, start over
            bool todayBad = Days.Count > 0 && Days[0].Date != today;
            bool tomorrowBad = Days.Count > 1 && Days[1].Date != tomorrow;
            if (todayBad || tomorrowBad || Days.Count > 2) 
                Days.Clear();
            bool anychanges = Days.Count != 2;

            //ensure today is at [0], tomorrow at [1]
            if (Days.Count == 0)
                Days.Add(new DayChunkSet { Date = today, Chunks = CreateDefaults(today, scheduledBoxes) });
            if (Days.Count == 1)
                Days.Add(new DayChunkSet { Date = tomorrow, Chunks = CreateDefaults(tomorrow, scheduledBoxes) });

            return anychanges;
        }

        /// <summary>
        /// Pack the contents of this container into database storage for Sttings.ChunkInfo column
        /// </summary>
        public string PackForStorage()
        {
            var segments = new List<string>();
            foreach (var day in Days)
            {
                bool first = true;
                foreach (var chunk in day.Chunks)
                {
                    string idList = "";
                    if (first) 
                        first = false;
                    else if (chunk.BoxIds != null) 
                        idList = string.Join(',', chunk.BoxIds);
                    string segment = day.Date + idList + ";" + chunk.Title.Replace('|', '_');
                    segments.Add(segment);
                }
            }
            return string.Join('|', segments);
        }

        /// <summary>
        /// create default chunks for a day that hasn't been chunked yet
        /// </summary>
        /// <param name="date">YYYYMMDD</param>
        /// <param name="scheduledBoxes">all cached scheduled boxes (includes all days)</param>
        List<Chunk> CreateDefaults(string date, IEnumerable<CachedBox> scheduledBoxes)
        {
            var defchunks = new List<Chunk>
            {
                new Chunk { Title = "Morning" },
                new Chunk { Title = "Afternoon" },
                new Chunk { Title = "Evening" }
            };

            var afternoonBoxIds = new List<long>();
            var eveningBoxIds = new List<long>();
            foreach (var box in scheduledBoxes.Where(r => r.BoxTime.StartsWith(date)))
            {
                int cidx = GetDefaultChunkIndex(box.BoxTime);
                if (cidx == 2) eveningBoxIds.Add(box.RowId);
                else if (cidx == 1) afternoonBoxIds.Add(box.RowId);
            }
            defchunks[1].BoxIds = afternoonBoxIds.ToArray();
            defchunks[2].BoxIds = eveningBoxIds.ToArray();
            return defchunks;
        }

        /// <summary>
        /// Return 0 if the given time is unparsable or before noon; 1 if afternoon; 2 if evening
        /// </summary>
        public static int GetDefaultChunkIndex(string time)
        {
            if (time == null || time.Length < 12) return 0;
            if (int.TryParse(time.Substring(8, 2), out int hr))
            {
                if (hr >= STARTEVENINGHR) return 2;
                else if (hr >= STARTAFTERNOONHR) return 1;
            }
            return 0;
        }
    }
}
