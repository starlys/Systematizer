using System;
using System.IO;
using System.Linq;
using System.Web;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    public static class HtmlExporter
    {
        /// <summary>
        /// Export people or boxes or both to readable HTML
        /// </summary>
        public static void ExportHtml(string fileName, bool inclAllPersons, long? includeThisCatPersons, bool inclTasks, bool inclNotes, bool inclPasswords)
        {
            using (var db = new SystematizerContext())
            using (var w = new StreamWriter(fileName))
            {
                //write style sheet
                w.Write("<html><head><style>body{font-family:trebuchet,arial}h1{background:tan;border-top:solid black 3px;font-size:1.5em}h2{font-size:1.1em}</style></head><body>");

                if (inclAllPersons || includeThisCatPersons != null)
                {
                    w.WriteHeading(1, "People");
                    IQueryable<Person> q;
                    if (inclAllPersons) q = db.Person;
                    else
                    {
                        var catids = Globals.AllCats.GetDescendantIds(includeThisCatPersons.Value);
                        q = db.Person.Where(r => db.PersonCat.Any(c => c.PersonId == r.RowId && catids.Contains(c.CatId)));
                    }
                    q = q.OrderBy(r => r.Name);
                    foreach (var person in q)
                    {
                        w.WriteHeading(2, person.Name);
                        w.StartTable();
                        w.TableRow("Phone", person.MainPhone);
                        w.TableRow("Email", person.MainEmail);
                        w.TableRow("Address", person.Address);
                        w.TableRow("Notes", person.Notes);
                        w.TableRow(Globals.PersonCustomLabels[0], person.Custom1);
                        w.TableRow(Globals.PersonCustomLabels[1], person.Custom2);
                        w.TableRow(Globals.PersonCustomLabels[2], person.Custom3);
                        w.TableRow(Globals.PersonCustomLabels[3], person.Custom4);
                        w.TableRow(Globals.PersonCustomLabels[4], person.Custom5);
                        w.EndTable();
                    }
                }

                if (inclTasks)
                {
                    w.WriteHeading(1, "Schedule");
                    var q = db.Box.Where(r => r.DoneDate == null && r.TimeType != 0).OrderBy(r => r.BoxTime);
                    foreach (var box in q)
                    {
                        w.WriteHeading(2, $"{DateUtil.ToReadableDate(box.BoxTime)} {DateUtil.ToReadableTime(box.BoxTime)} -- {box.Title}");
                        WriteBoxDetail(inclPasswords, w, box);
                    }
                }

                if (inclNotes)
                {
                    w.WriteHeading(1, "Notes");
                    var q = db.Box.Where(r => r.DoneDate == null && r.TimeType == 0 && r.ParentId == null).OrderBy(r => r.Title).ToArray();
                    foreach (var box in q)
                    {
                        WriteNoteBoxWithChildren(db, 2, inclPasswords, w, box);
                    }
                }

                //end document
                w.Write("</body></html>");
                w.Flush();
            }
        }

        static void WriteNoteBoxWithChildren(SystematizerContext db, int recurLevel, bool inclPasswords, StreamWriter w, Box box)
        {
            w.WriteHeading(Math.Min(recurLevel, 5), box.Title);
            WriteBoxDetail(inclPasswords, w, box);
            var children = db.Box.Where(r => r.ParentId == box.RowId).OrderBy(r => r.Title).ToArray();
            if (children.Any())
            {
                w.Write("<div style=\"margin-left:8px;border-left:solid black 1px\">");
                foreach (var child in children)
                    WriteNoteBoxWithChildren(db, recurLevel + 1, inclPasswords, w, child);
                w.Write("</div>");
            }
        }

        static void WriteBoxDetail(bool inclPasswords, StreamWriter w, Box box)
        {
            w.StartTable();
            w.TableRow("Notes", box.Notes);
            w.TableRow("Folder", box.RefDir);
            w.TableRow("File", box.RefFile);
            if (inclPasswords) w.TableRow("Password", box.Password);
            w.EndTable();
        }

        static void WriteHeading(this StreamWriter w, int level, string text)
        {
            w.Write($"<h{level}>{HttpUtility.HtmlEncode(text)}</h{level}>");
        }

        static void StartTable(this StreamWriter w)
        {
            w.Write($"<table><tbody>");
        }

        static void EndTable(this StreamWriter w)
        {
            w.Write($"</tbody></table>");
        }

        static void TableRow(this StreamWriter w, string caption, string value)
        {
            if (string.IsNullOrEmpty(caption) || string.IsNullOrEmpty(value)) return;
            w.Write($"<tr><td>{HttpUtility.HtmlEncode(caption)}: </td><td>{HttpUtility.HtmlEncode(value.Replace("\r\n", " / "))}</td></tr>");
        }

    }
}
