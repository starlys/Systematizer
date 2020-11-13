using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    public static class DBUtil
    {
        /// <summary>
        /// read settings and categories into globals
        /// </summary>
        internal static void ReadSettings()
        {
            using (var db = new SystematizerContext())
            {
                var settings = db.Setting.First();
                Globals.PersonCustomLabels = new[] { settings.Custom1Label, settings.Custom2Label, settings.Custom3Label, settings.Custom4Label, settings.Custom5Label };
                Globals.DayChunks = new MultiDayChunkSet();
                Globals.DayChunks.Initialize(settings.ChunkInfo ?? "");
                Globals.AllowTasks = settings.AllowTasks != 0;

                Globals.AllCats = new CatCache(db.Cat);
            }
        }

        /// <summary>
        /// Shortcut for saving settings; caller provides function to change the needed values
        /// </summary>
        public static void WriteSettings(Action<Setting> modify)
        {
            using (var db = new SystematizerContext())
            {
                var settings = db.Setting.First();
                modify(settings);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// In a new context, attach a record (detecting if new or modified) and save it.
        /// Caller may also inject other actions on the context.
        /// </summary>
        /// <param name="beforeWrite">optional action to run before writing record; this can return false to abandon changes</param>
        /// <param name="afterWrite">optional action to run after writing record; any context changes are saved again after this is run</param>
        internal static void WriteAny(BaseTable record, Func<SystematizerContext, bool> beforeWrite, Action<SystematizerContext> afterWrite)
        {
            using (var db = new SystematizerContext())
            {
                if (beforeWrite?.Invoke(db) == false) return;
                var entry = db.Attach(record);
                entry.State = record.RowId == 0 ? EntityState.Added : EntityState.Modified; 
                db.SaveChanges();
                afterWrite?.Invoke(db);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// write full text index for a box; does not save changes
        /// </summary>
        internal static void WriteBoxIndex(SystematizerContext db, Box box)
        {
            var fullTextIndex = new FullTextManager();
            fullTextIndex.TitleToIndex.AddUserField(box.Title);
            fullTextIndex.DetailsToIndex.AddUserField(box.Notes);
            fullTextIndex.DetailsToIndex.AddUserField(box.RawEmail);
            fullTextIndex.WriteIndex(db, 0, box.RowId);
        }

        /// <summary>
        /// write full text index for a person; does not save changes
        /// </summary>
        internal static void WritePersonIndex(SystematizerContext db, Person person)
        {
            var fullTextIndex = new FullTextManager();
            fullTextIndex.TitleToIndex.AddUserField(person.Name);
            fullTextIndex.DetailsToIndex.AddUserField(person.MainEmail);
            fullTextIndex.DetailsToIndex.AddUserField(person.MainPhone);
            fullTextIndex.DetailsToIndex.AddUserField(person.Address);
            fullTextIndex.DetailsToIndex.AddUserField(person.Notes);
            fullTextIndex.DetailsToIndex.AddUserField(person.Custom1);
            fullTextIndex.DetailsToIndex.AddUserField(person.Custom2);
            fullTextIndex.DetailsToIndex.AddUserField(person.Custom3);
            fullTextIndex.DetailsToIndex.AddUserField(person.Custom4);
            fullTextIndex.DetailsToIndex.AddUserField(person.Custom5);
            fullTextIndex.WriteIndex(db, 1, person.RowId);
        }

        /// <summary>
        /// Build an exists subquery for filtering a parent record where a keyword exists in the word index.
        /// Return value is in the form "exists(select..)"
        /// </summary>
        /// <param name="tableName">Box or Person</param>
        /// <param name="word">user-entered word</param>
        /// <param name="wordKind">see Word.Kind column</param>
        /// <returns>null if word if not searchable</returns>
        internal static string BuildKeywordFilter(string tableName, int wordKind, string word, bool includeDetails)
        {
            word = IndexableWordSet.NormalizeWord(word);
            if (word == null) return null;

            //make next word that is one more potential word than is being searched. Eg. HAPPY => HAPPZ
            var word2 = new StringBuilder(word);
            int lastIdx = word2.Length - 1;
            word2[lastIdx] = ++word2[lastIdx];

            string detailClause = includeDetails ? "" : " and IsDetail=0";
            return $"exists(select * from Word where ParentId={tableName}.RowId and Kind={wordKind} {detailClause} and Word8>='{word}' and Word8<'{word2}')";
        }

        /// <summary>
        /// Load the given subset of boxes and project into CachedBox format
        /// </summary>
        internal static List<CachedBox> LoadForCaching(IQueryable<Box> scheduledBoxes)
        {
            //note code duplication with BoxCache because LINQ might not be smart enough to do the Select in SQL if it calls another function
            var list = scheduledBoxes.Select(r => new CachedBox
            {
                BoxTime = r.BoxTime,
                DoneDate = r.DoneDate,
                Duration = r.Duration,
                Importance = r.Importance,
                IsUnclass = r.IsUnclass,
                ParentId = r.ParentId,
                PrepDuration = r.PrepDuration,
                RepeatInfo = r.RepeatInfo,
                RowId = r.RowId,
                TimeType = r.TimeType,
                Title = r.Title,
                Visibility = r.Visibility,
                SmallNotes = r.Notes
            }).ToList();

            //truncate notes
            foreach (var box in list) box.TruncateSmallNotes();
            return list;
        }

        /// <summary>
        /// Load boxes matching keyword filter and other filters (max 100 results)
        /// </summary>
        /// <param name="doneSince">if null, searches not-done only; if set, searches done items since this date YYYYMMDD</param>
        /// <returns>can be null or empty collection</returns>
        internal static IEnumerable<CachedBox> BoxesByKeyword(SystematizerContext db, string term, bool includeDetails, string doneSince)
        {
            string keyFilter = BuildKeywordFilter("Box", 0, term, includeDetails);
            bool doneMode = doneSince != null;
            string doneFilter = doneMode ? $"DoneDate is not null and DoneDate>='{doneSince}'" : "DoneDate is null";
            if (keyFilter == null && !doneMode) return null;
            string sql = $"select RowId,* from Box where {doneFilter}";
            if (keyFilter != null) sql += $" and {keyFilter}";
            sql += " limit 100";
            return LoadForCaching(db.Box.FromSqlRaw(sql));
        }

        internal static IEnumerable<CachedBox> LoadBoxesByParent(SystematizerContext db, long parentId)
        {
            return LoadForCaching(db.Box.Where(r => r.ParentId == parentId).OrderBy(r => r.Title));
        }

        internal static long[] BoxesWithChildren(SystematizerContext db, long[] ids)
        {
            var boxIdsWithChildren = db.Box.Where(b => ids.Contains(b.RowId) && db.Box.Any(b2 => b2.ParentId == b.RowId)).Select(b => b.RowId).ToArray();
            return boxIdsWithChildren;
        }

        /// <summary>
        /// Load persons matching keyword filter and optional category filter
        /// </summary>
        /// <param name="catIds">null or catIds to match (using AND)</param>
        /// <returns>can be null or empty collection</returns>
        internal static IEnumerable<Person> LoadFilteredPersons(SystematizerContext db, string keyword, bool lookInDetails, long[] catIds,
            bool allowLoadUnfiltered = false, bool limit100 = false)
        {
            string keyFilter = BuildKeywordFilter("Person", 1, keyword, lookInDetails);
            var filters = new List<string>(2);
            if (keyFilter != null) filters.Add(keyFilter);
            if (catIds != null)
            {
                foreach (long catId in catIds)
                {
                    var catids = Globals.AllCats.GetDescendantIds(catId);
                    string inClauseValues = string.Join(',', catids);
                    filters.Add($"exists(select * from PersonCat where PersonId=Person.RowId and CatId in ({inClauseValues}))");
                }
            }
            if (filters.Count == 0 && !allowLoadUnfiltered) return null;
            string combinedFilter = "";
            if (filters.Count > 0) combinedFilter = " where " + string.Join(" and ", filters);

            string sql = $"select RowId,* from Person {combinedFilter}";
            if (limit100) sql += " limit 100";

            return db.Person.FromSqlRaw(sql).OrderBy(r => r.Name).ToArray();
        }

        /// <summary>
        /// Delete a box which was loaded in the given context, cascading to child records
        /// </summary>
        internal static void DeleteBox(SystematizerContext db, Box box)
        {
            db.Database.ExecuteSqlRaw($"delete from BoxPerson where BoxId={box.RowId}");
            db.Database.ExecuteSqlRaw($"delete from Word where Kind=0 and ParentId={box.RowId}");
            db.Box.Remove(box);
            db.SaveChanges();
        }

        /// <summary>
        /// Delete a person, cascading to child records
        /// </summary>
        internal static void DeletePerson(SystematizerContext db, long personId)
        {
            db.Database.ExecuteSqlRaw($"delete from PersonPerson where Person1Id={personId}");
            db.Database.ExecuteSqlRaw($"delete from PersonPerson where Person2Id={personId}");
            db.Database.ExecuteSqlRaw($"delete from PersonCat where PersonId={personId}");
            db.Database.ExecuteSqlRaw($"delete from BoxPerson where PersonId={personId}");
            db.Database.ExecuteSqlRaw($"delete from Word where Kind=1 and ParentId={personId}");
            db.Database.ExecuteSqlRaw($"delete from Person where RowId={personId}");
        }

        /// <summary>
        /// Delete boxes older than one year after they are done
        /// </summary>
        internal static void DeleteVeryOldBoxes()
        {
            using (var db = new SystematizerContext())
            {
                DateTime cutoff = DateTime.Today.AddYears(-1);
                string cutoffS = DateUtil.ToYMD(cutoff);
                var boxes = db.Box.FromSqlRaw($"select RowId,* from Box where DoneDate is not null and DoneDate < '{cutoffS}'");
                db.Box.RemoveRange(boxes);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// add and delete PersonCat records so they match the given list for the person
        /// </summary>
        internal static void SavePersonCats(SystematizerContext db, long personId, long[] selectedCatIds)
        {
            var existing = db.PersonCat.Where(r => r.PersonId == personId).ToArray();

            //remove deleted ones
            foreach (var pc in existing)
                if (!selectedCatIds.Contains(pc.CatId))
                    db.PersonCat.Remove(pc);

            //add new ones
            foreach (long id in selectedCatIds)
                if (!existing.Any(r => r.CatId == id))
                    db.PersonCat.Add(new PersonCat { PersonId = personId, CatId = id });

            db.SaveChanges();
        }

        /// <summary>
        /// True if the box given by boxId,parentId has circular parentage or is nested more than 20 levels
        /// </summary>
        internal static bool HasCircularParentage(SystematizerContext db, long boxId, long? parentId)
        {
            if (parentId == null) return false;
            if (parentId.Value == boxId) return true;
            var encountered = new HashSet<long>();
            encountered.Add(boxId);
            encountered.Add(parentId.Value);
            for (int i = 0; i < 20; ++i)
            {
                parentId = db.Box.Where(r => r.RowId == parentId.Value).Select(r => r.ParentId).FirstOrDefault();
                if (parentId == null) return false;
                if (encountered.Contains(parentId.Value)) return true;
                encountered.Add(parentId.Value);
            }
            return true;
        }

        /// <summary>
        /// With deferred execution, get all kinds of links from the given box.
        /// </summary>
        internal static IEnumerable<LinkRecord> LoadLinksFor(SystematizerContext db, Box box)
        {
            //parent box
            if (box.ParentId != null)
            {
                var parent = db.Box.Find(box.ParentId);
                if (parent != null)
                    yield return new LinkRecord
                    {
                        Link = LinkType.FromBoxToParentBox,
                        OtherId = box.ParentId.Value,
                        Description = parent.Title
                    };
            }

            //child boxes
            var childBoxes = db.Box.Where(r => r.ParentId == box.RowId).Select(r => new { r.RowId, r.Title });
            foreach (var b2 in childBoxes)
                yield return new LinkRecord
                {
                    Link = LinkType.FromBoxToChildBox,
                    OtherId = b2.RowId,
                    Description = b2.Title
                };

            //linked persons
            var persons = db.Person.FromSqlRaw("select Person.RowId,Name from Person inner join BoxPerson on Person.RowId=BoxPerson.PersonId where BoxId=" + box.RowId)
                .Select(r => new { r.RowId, r.Name });
            foreach (var person in persons)
                yield return new LinkRecord
                {
                    Link = LinkType.FromBoxToPerson,
                    OtherId = person.RowId,
                    Description = person.Name
                };
        }

        /// <summary>
        /// With deferred execution, get all kinds of links from the given person.
        /// </summary>
        internal static IEnumerable<LinkRecord> LoadLinksFor(SystematizerContext db, Person person)
        {
            //boxes
            var boxes = db.Box.FromSqlRaw("select Box.RowId,Title from Box inner join BoxPerson on Box.RowId=BoxPerson.BoxId where PersonId=" + person.RowId)
                .Select(r => new { r.RowId, r.Title });
            foreach (var b2 in boxes)
                yield return new LinkRecord
                {
                    Link = LinkType.FromPersonToBox,
                    OtherId = b2.RowId,
                    Description = b2.Title
                };

            //persons
            var persons = db.Person.FromSqlRaw("select Person.RowId,Name from Person inner join PersonPerson on Person.RowId=PersonPerson.Person1Id where Person2Id=" + person.RowId)
                .Select(r => new { r.RowId, r.Name });
            foreach (var person2 in persons)
                yield return new LinkRecord
                {
                    Link = LinkType.FromPersonToPerson,
                    OtherId = person2.RowId,
                    Description = person2.Name
                };
        }

        /// <summary>
        /// Add or delete a link between any two records involving a person (ignores other types)
        /// </summary>
        internal static void WritePersonLink(SystematizerContext db, LinkInstruction cmd)
        {
            //sort out what to do
            bool writeBoxPerson = false, writePersonPerson = false;
            long boxId = 0, person1Id = 0, person2Id = 0;
            if (cmd.Link == LinkType.FromBoxToPerson)
            {
                writeBoxPerson = true;
                boxId = cmd.FromId;
                person1Id = cmd.ToId;
            }
            else if (cmd.Link== LinkType.FromPersonToBox)
            {
                writeBoxPerson = true;
                person1Id = cmd.FromId;
                boxId = cmd.ToId;
            }
            else if (cmd.Link== LinkType.FromPersonToPerson)
            {
                writePersonPerson = true;
                person1Id = cmd.FromId;
                person2Id = cmd.ToId;
            }

            //write BoxPerson
            if (writeBoxPerson)
            {
                var bp = db.BoxPerson.FirstOrDefault(r => r.BoxId == boxId && r.PersonId == person1Id);
                if (bp == null && !cmd.IsRemove)
                {
                    bp = new BoxPerson { BoxId = boxId, PersonId = person1Id };
                    db.BoxPerson.Add(bp);
                }
                if (bp != null && cmd.IsRemove)
                {
                    db.BoxPerson.Remove(bp);
                }
            }

            //write PersonPerson
            if (writePersonPerson)
            {
                var pp = db.PersonPerson.FirstOrDefault(r => r.Person1Id == person1Id && r.Person2Id == person2Id);
                if (pp == null && !cmd.IsRemove)
                {
                    pp = new PersonPerson { Person1Id = person1Id, Person2Id = person2Id };
                    db.PersonPerson.Add(pp);
                }
                if (pp != null && cmd.IsRemove)
                {
                    db.PersonPerson.Remove(pp);
                }
                var ppBack = db.PersonPerson.FirstOrDefault(r => r.Person1Id == person2Id && r.Person2Id == person1Id);
                if (ppBack == null)
                {
                    ppBack = new PersonPerson { Person1Id = person2Id, Person2Id = person1Id };
                    db.PersonPerson.Add(ppBack);
                }
                if (ppBack != null && cmd.IsRemove)
                {
                    db.PersonPerson.Remove(ppBack);
                }
            }
            db.SaveChanges();
        }
    }
}
