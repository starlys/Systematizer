using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    /// <summary>
    /// Convert persistent types to/from CSV
    /// </summary>
    public class CsvConverter
    {
        const string COMMA = ",", QUOTE = "\"";

        public IEnumerable<string> ToCsv(IEnumerable<Person> persons)
        {
            yield return "Name,MainPhone,MainEmail,Address,Notes,Custom1,Custom2,Custom3,Custom4,Custom5";
            foreach (var person in persons)
                yield return AsQuoted(person.Name)
                    + COMMA + AsQuoted(person.MainPhone)
                    + COMMA + AsQuoted(person.MainEmail)
                    + COMMA + AsQuoted(person.Address)
                    + COMMA + AsQuoted(person.Notes)
                    + COMMA + AsQuoted(person.Custom1)
                    + COMMA + AsQuoted(person.Custom2)
                    + COMMA + AsQuoted(person.Custom3)
                    + COMMA + AsQuoted(person.Custom4)
                    + COMMA + AsQuoted(person.Custom5);
        }

        public Person[] PersonFromCsv(TextReader rdr)
        {
            using var parser = new CsvHelper.CsvReader(rdr, CultureInfo.InvariantCulture);
            parser.Configuration.HeaderValidated = null;
            parser.Configuration.MissingFieldFound = null;
            parser.Configuration.IgnoreBlankLines = false;
            var ret = parser.GetRecords<Person>();
            return ret.ToArray();
        }

        public IEnumerable<string> ToCsv(IEnumerable<Box> boxes)
        {
            yield return "TimeType,Importance,Visibility,BoxTime,Title,Duration,PrepDuration,Notes,RefDir,RefFile,Password,RepeatInfo";
            foreach (var box in boxes)
                yield return box.TimeType
                    + COMMA + box.Importance
                    + COMMA + box.Visibility
                    + COMMA + AsQuoted(box.BoxTime)
                    + COMMA + AsQuoted(box.Title)
                    + COMMA + AsQuoted(box.Duration)
                    + COMMA + AsQuoted(box.PrepDuration)
                    + COMMA + AsQuoted(box.Notes)
                    + COMMA + AsQuoted(box.RefDir)
                    + COMMA + AsQuoted(box.RefFile)
                    + COMMA + AsQuoted(box.Password)
                    + COMMA + AsQuoted(box.RepeatInfo);
        }

        public Box[] BoxFromCsv(TextReader rdr)
        {
            using var parser = new CsvHelper.CsvReader(rdr, CultureInfo.InvariantCulture);
            parser.Configuration.HeaderValidated = null;
            parser.Configuration.MissingFieldFound = null;
            parser.Configuration.IgnoreBlankLines = false;
            var ret = parser.GetRecords<Box>().ToArray();
            foreach (var b in ret)
            {
                if (b.BoxTime == "") b.BoxTime = null;
            }
            return ret;
        }

        string AsQuoted(string s)
        {
            if (s == null) return "";
            if (!s.Contains(QUOTE) && !s.Contains('\n')) return s;
            s = s.Replace(QUOTE, $"{QUOTE}{QUOTE}");
            return $"{QUOTE}{s}{QUOTE}";
        }
    }
}
