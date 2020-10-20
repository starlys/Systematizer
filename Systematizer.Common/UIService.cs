using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    /// <summary>
    /// Landing spot for required calls from UI layer
    /// </summary>
    public class UIService
    {
        /// <summary>
        /// App startup (call when UI loaded and after database is identified); true if file opened
        /// </summary>
        /// <param name="path">path and file name of database</param>
        public bool OpenDatabase(string path)
        {
            if (!File.Exists(path)) return false;
            Globals.DatabasePath = path;
            Globals.Connection = new ConnectionWrapper();
            Globals.BoxCache = new BoxCache();
            Globals.BoxCache.Initialize();
            DBUtil.ReadSettings();
            DBUtil.DeleteVeryOldBoxes();
            RequestWakeUp();
            return true;
        }

        /// <summary>
        /// App shutdown; also called when user has selected a different database; implementation cleans up resources
        /// </summary>
        public void CloseDatabase()
        {
            Globals.BoxCache = null;
            Globals.Connection?.Dispose();
            Globals.Connection = null;
            Globals.DatabasePath = null;
        }

        /// <summary>
        /// Perform timed activities; UI should call this every 30s
        /// </summary>
        /// <param name="idleSeconds">seconds since last user activity</param>
        public void Ping30(int idleSeconds)
        {
            //idle tests
            if (idleSeconds > 30)
                Globals.Connection?.NotifyIdle();
            if (!Globals.UIState.IsIdle && idleSeconds > 60 * 15)
            {
                Globals.UIAction.SetIdleMode(true);
            }
            Reminderer.CheckAndSend();
        }

        /// <summary>
        /// Call when user requests wake up app from idle mode; pending edits should be saved before calling this (so it doesn't overwrite day chunks)
        /// </summary>
        public void RequestWakeUp()
        {
            Globals.UIState.IsIdle = false;
            Globals.BoxCache.AutoCompleteTasks();
            if (Globals.DayChunks.ResetForToday(Globals.BoxCache.GetScheduledBoxes()))
                SaveDayChunks();
            Globals.UIAction.SetIdleMode(false);
        }

        /// <summary>
        /// Write Globals.DayChunks to database; caller should ensure that object is up to date before calling this
        /// </summary>
        public void SaveDayChunks()
        {
            string dbValue = Globals.DayChunks.PackForStorage();
            DBUtil.WriteSettings(s => s.ChunkInfo = dbValue); 
        }

        /// <summary>
        /// Get boxes for detail view or editing; caller should call SaveBox or AbandonBox when the pane/window is closed
        /// </summary>
        public IEnumerable<ExtBox> LoadUnclassBoxesForEditing()
        {
            var ret = new List<ExtBox>();
            using (var db = new SystematizerContext())
            {
                var boxes = db.Box.Where(r => r.IsUnclass != 0);
                foreach (var box in boxes)
                {
                    Globals.BoxEditingPool.CheckOut(box);
                    var links = DBUtil.LoadLinksFor(db, box).ToList();
                    ret.Add(new ExtBox(box, links));
                }
            }
            return ret;
        }

        /// <summary>
        /// Check if boxes exist matching the criteria given.
        /// </summary>
        /// <param name="parentRowId">matches on parent ID - required</param>
        public bool CheckBoxesExist(long parentRowId = 0, bool filterByNotDone = false)
        {
            if (parentRowId == 0) throw new Exception("invalid arguments");
            using (var db = new SystematizerContext())
            {
                IQueryable<Box> boxes = db.Box.Where(r => r.ParentId == parentRowId);
                if (filterByNotDone) boxes = boxes.Where(r => r.DoneDate == null);
                return boxes.Any();
            }
        }

        /// <summary>
        /// Get box for detail view or editing; caller should call SaveBox or AbandonBox when the pane/window is closed
        /// </summary>
        /// <returns>null if not found</returns>
        public ExtBox LoadBoxForEditing(long boxId)
        {
            using (var db = new SystematizerContext())
            {
                var box = db.Box.Find(boxId);
                if (box == null) return null;
                Globals.BoxEditingPool.CheckOut(box);
                var links = DBUtil.LoadLinksFor(db, box).ToList();
                return new ExtBox(box, links);
            }
        }

        /// <summary>
        /// Reload the links in a box; to be called after the UI records changes in links
        /// </summary>
        public void UpdateLinks(ExtBox ebox)
        {
            using (var db = new SystematizerContext())
            {
                var links = DBUtil.LoadLinksFor(db, ebox.Box).ToList();
                ebox.Links = links;
            }
        }

        /// <summary>
        /// Reload the links in a box; to be called after the UI records changes in links
        /// </summary>
        public void UpdateLinks(ExtPerson ep)
        {
            using (var db = new SystematizerContext())
            {
                var links = DBUtil.LoadLinksFor(db, ep.Person).ToList();
                ep.Links = links;
            }
        }

        /// <summary>
        /// Get boxes for export
        /// </summary>
        /// <param name="ids">either null to include all non-done boxes or a specific list</param>
        public Box[] LoadBoxesForExport(long[] ids)
        {
            using (var db = new SystematizerContext())
            {
                if (ids == null)
                    return db.Box.Where(r => r.DoneDate == null).ToArray();
                return db.Box.Where(r => ids.Contains(r.RowId)).ToArray();
            }
        }

        /// <summary>
        /// Get persons for export
        /// </summary>
        /// <param name="ids">either null to include all or a specific list</param>
        public Person[] LoadPersonsForExport(long[] ids)
        {
            using (var db = new SystematizerContext())
            {
                if (ids == null)
                    return db.Person.ToArray();
                return db.Person.Where(r => ids.Contains(r.RowId)).ToArray();
            }
        }

        public CachedBox[] LoadBoxesByParent(long parentId)
        {
            using (var db = new SystematizerContext())
            {
                return DBUtil.LoadBoxesByParent(db, parentId).ToArray();
            }
        }

        /// <summary>
        /// Get boxes for done/search blocks; see comments in DBUtil
        /// </summary>
        public CachedBox[] LoadBoxesByKeyword(string term, bool includeDetails, string doneSince)
        {
            using (var db = new SystematizerContext())
            {
                var boxes = DBUtil.BoxesByKeyword(db, term, includeDetails, doneSince);
                if (boxes == null) return null;
                return boxes.ToArray();
            }
        }

        /// <summary>
        /// Get persons for search block; see comments in DBUtil
        /// </summary>
        public Person[] LoadFilteredPersons(string term, bool includeDetails, long? catId)
        {
            using (var db = new SystematizerContext())
            {
                var a = DBUtil.LoadFilteredPersons(db, term, includeDetails, catId);
                if (a == null) a = new Person[0];
                return a.ToArray();
            }
        }

        /// <summary>
        /// Validate and save box, keeping cache up to date.
        /// Saves from ExtBox.Repeats, instead of directly from Box.RepeatInfo!
        /// throws exception on validation/save problem
        /// </summary>
        /// <returns>new rowID</returns>
        public long SaveBox(ExtBox ebox, bool propagageToUI)
        {
            //validate
            string message = ebox.Box.Validate();
            if (message != null) throw new Exception("Invalid task/note: " + message);
            if (!Globals.AllowTasks && ebox.Box.TimeType != Constants.TIMETYPE_NONE) throw new Exception("cannot store scheduled task in database not allowing tasks");
            if (ebox.Box.TimeType != Constants.TIMETYPE_NONE && string.IsNullOrEmpty(ebox.Box.BoxTime))
                throw new Exception("Scheduled tasks must have the date/time set");

            //finalize storage formats
            ebox.Box.RepeatInfo = ebox.Repeats?.PackForStorage();

            //save
            DBUtil.WriteAny(ebox.Box, db =>
            {
                if (DBUtil.HasCircularParentage(db, ebox.Box.RowId, ebox.Box.ParentId)) return false;
                return true;
            },
            db =>
            {
                DBUtil.WriteBoxIndex(db, ebox.Box);
            });

            //update cache
            var changes = Globals.BoxEditingPool.CheckIn(ebox.Box);
            Globals.BoxCache.UpdateCacheAfterSave(ebox.Box, changes, propagageToUI);
            return ebox.Box.RowId;
        }

        public void AbandonBox(long boxId)
        {
            Globals.BoxEditingPool.Abandon(boxId);
        }

        /// <summary>
        /// Return a subset of rowIds given in the argument, including only those Box ids that have child boxes
        /// </summary>
        public long[] BoxesWithChildren(long[] ids)
        {
            if (ids.Length == 0) return new long[0];
            using (var db = new SystematizerContext())
            {
                return DBUtil.BoxesWithChildren(db, ids);
            }
        }

        /// <summary>
        /// Load person detail for editing
        /// </summary>
        /// <returns>null if not found</returns>
        public ExtPerson LoadPerson(long id)
        {
            using (var db = new SystematizerContext())
            {
                var person = db.Person.Find(id);
                if (person == null) return null;
                var links = DBUtil.LoadLinksFor(db, person).ToList();
                var catIds = db.PersonCat.Where(r => r.PersonId == id).Select(r => r.CatId);
                return new ExtPerson(person, links, catIds.ToArray());
            }
        }

        /// <summary>
        /// Validate and save person
        /// throws exception on validation/save problem
        /// </summary>
        /// <returns>new rowID</returns>
        public long SavePerson(ExtPerson person)
        {
            //validate
            string message = person.Person.Validate();
            if (message != null) throw new Exception("Invalid person record: " + message);

            //save
            DBUtil.WriteAny(person.Person, null, db =>
            {
                DBUtil.WritePersonIndex(db, person.Person);
                DBUtil.SavePersonCats(db, person.Person.RowId, person.SelectedCatIds);
            });

            //update cache
            return person.Person.RowId;
        }

        /// <summary>
        /// Add or delete a link between any two records involving a person (ignores other types)
        /// </summary>
        public void WritePersonLink(LinkInstruction cmd)
        {
            using (var db = new SystematizerContext())
            {
                DBUtil.WritePersonLink(db, cmd);
            }
        }

        /// <summary>
        /// Add or Modify a category and update cache
        /// </summary>
        /// <param name="rowId">pass 0 to add new cat</param>
        /// <param name="modify">function to modify it</param>
        public void ModifyCat(long rowId, Action<Cat> modify)
        {
            using (var db = new SystematizerContext())
            {
                Cat cat;
                if (rowId == 0)
                {
                    cat = new Cat();
                    db.Cat.Add(cat);
                }
                else
                    cat = db.Cat.Find(rowId);
                if (cat == null) return;
                modify(cat);
                db.SaveChanges();
                Globals.AllCats = new CatCache(db.Cat);
            }
        }

        /// <summary>
        /// Get a warning message about deleting a category; call this before calling
        /// DeleteCat. Returns null string if no warning needed.
        /// </summary>
        /// <returns>true if allowed to delete, and message to show user</returns>
        public (bool, string) GetCategoryDeleteWarning(long rowId)
        {
            var cat = Globals.AllCats.Find(rowId);
            if (cat == null) 
                return (false, "No such category");
            if (cat.Children != null && cat.Children.Any())
                return (false, "Category cannot be deleted because it has sub-categories. Delete the sub-categories first.");
            using (var db = new SystematizerContext())
            {
                int n = db.PersonCat.Count(r => r.CatId == rowId);
                if (n == 0)
                    return (true, null);
                if (cat.Parent == null)
                    return (true, $"Deleting top level. {n} records will have the category removed.");
                else
                    return (true, $"Deleting sub-category. {n} records will be promoted to the containing category.");
            }
        }

        /// <summary>
        /// Delete a category, removing or promoting references to that category; updates cache
        /// </summary>
        /// <param name="rowId"></param>
        public void DeleteCategory(long rowId)
        {
            using (var db = new SystematizerContext())
            {
                var catRec = db.Cat.Find(rowId);
                if (catRec == null) return;
                db.Cat.Remove(catRec);
                long? promoteTo = null;
                if (catRec.ParentId != null) promoteTo = catRec.ParentId;

                var q = db.PersonCat.Where(r => r.CatId == rowId).ToArray();
                foreach (var link in q)
                {
                    if (promoteTo == null)
                        db.PersonCat.Remove(link);
                    else
                    {
                        var existing = db.PersonCat.FirstOrDefault(r => r.PersonId == link.PersonId && r.CatId == promoteTo.Value);
                        if (existing == null)
                            db.PersonCat.Add(new PersonCat { PersonId = link.PersonId, CatId = promoteTo.Value });
                    }
                }
                db.SaveChanges();
                Globals.AllCats = new CatCache(db.Cat);
            }
        }

        public void DeletePerson(long rowId)
        {
            using (var db = new SystematizerContext())
            {
                DBUtil.DeletePerson(db, rowId);
            }    
        }

        /// <summary>
        /// return the box or its first ancestor box where the predicate is true; or null
        /// </summary>
        public Box NavigateToParentBoxWhere(Box box, Func<Box, bool> predicate)
        {
            if (predicate(box)) return box;
            if (box.ParentId == null) return null;
            using (var db = new SystematizerContext())
            {
                for (int i = 0; i < 10; ++i)
                {
                    box = db.Box.Find(box.ParentId);
                    if (box == null) return null;
                    if (predicate(box)) return box;
                    if (box.ParentId == null) return null;
                }
            }
            return null;
        }
    }
}
