using Systematizer.Common.PersistentModel;

namespace Systematizer.Common;

/// <summary>
/// Cache of certain boxes for fast display, and projections of repeating tasks.
/// </summary>
public class BoxCache
{
    List<CachedBox> ScheduledBoxes, TopNotes;
    readonly List<AgendaEntry> Agenda = new (); //always keep this sorted by time

    /// <summary>
    /// Load cache
    /// </summary>
    public void Initialize()
    {
        using (var db = new SystematizerContext())
        {
            ScheduledBoxes = DBUtil.LoadForCaching(db.Box.Where(r => r.BoxTime != null && r.DoneDate == null));
            //note code duplication with next line and UpdateCacheAfterSave
            TopNotes = DBUtil.LoadForCaching(db.Box.Where(r => r.BoxTime == null && r.ParentId == null && r.IsUnclass == 0 && r.DoneDate == null));
        }

        foreach (var box in ScheduledBoxes)
        {
            box.Repeats = ParsedRepeatInfo.Build(box.RepeatInfo);
            Agenda.AddRange(RepeatProjector.Project(box, true, true));
        }
        AssignHighlights();
        SortAgenda();
        SortTopNotes();
    }

    void SortAgenda()
    {
        Agenda.Sort((i, j) => i.Time.CompareTo(j.Time));
    }

    void SortTopNotes()
    {
        TopNotes.Sort((i, j) => i.Title.CompareTo(j.Title));
    }

    public IEnumerable<long> GetCachedBoxIds()
    {
        return ScheduledBoxes.Select(r => r.RowId).Union(TopNotes.Select(r => r.RowId));
    }

    public IEnumerable<CachedBox> GetScheduledBoxes() => ScheduledBoxes;
    public IEnumerable<CachedBox> GetTopNotes() => TopNotes;

    /// <summary>
    /// Get agenda in time order including repeat projections
    /// </summary>
    public IEnumerable<AgendaEntry> GetAgenda() => Agenda;

    static CachedBox FullBoxToCachedBox(Box r)
    {
        var cb = new CachedBox
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
            SmallNotes = r.Notes,
            Repeats = ParsedRepeatInfo.Build(r.RepeatInfo)
        };
        cb.TruncateSmallNotes();
        return cb;
    }

    /// <summary>
    /// Mark any tasks done that are low priority and were scheduled in the past; save changes
    /// </summary>
    internal void AutoCompleteTasks()
    {
        DateTime cutoff = DateTime.Today;
        string cutoffS = DateUtil.ToYMD(cutoff);
        var toDelete = new List<long>();
        for (int i = ScheduledBoxes.Count - 1; i >= 0; --i)
        {
            var box = ScheduledBoxes[i];
            if (box.Importance == Constants.IMPORTANCE_LOW && box.BoxTime != null && DateUtil.IsBefore(box.BoxTime, cutoffS))
            {
                ScheduledBoxes.RemoveAt(i);
                toDelete.Add(box.RowId);
            }
        }

        if (toDelete.Count == 0) return;
        using var db = new SystematizerContext();
        string rowids = string.Join(',', toDelete);
        db.Database.ExecuteSqlRaw($"update Box set DoneDate='{cutoffS}' where RowId in ({rowids})");
    }

    /// <summary>
    /// refreshes cache with the current version of the box; call this after saving changes to a box
    /// </summary>
    public void UpdateCacheAfterSave(Box box, BoxEditingPool.Item changes, bool propagateToUI)
    {
        //determine what was changed
        bool newIsHighlighted = box.Visibility == Constants.VISIBILITY_HIGHLIGHT;

        //update cache
        (bool _, bool oldWasHighlighted) = ForgetAbout(box.RowId);
        if (box.BoxTime != null && box.DoneDate == null)
        {
            var cb = FullBoxToCachedBox(box);
            ScheduledBoxes.Add(cb);
            Agenda.AddRange(RepeatProjector.Project(cb, true, true));
            SortAgenda();
        }
        else if (box.BoxTime == null && box.ParentId == null && box.IsUnclass == 0 && box.DoneDate == null)
        {
            var cb = FullBoxToCachedBox(box);
            TopNotes.Add(cb);
            SortTopNotes();
        }

        //propagate changes to UI
        if (oldWasHighlighted || newIsHighlighted) AssignHighlights();
        if (propagateToUI) Globals.UIAction.BoxCacheChanged(changes);
    }

    /// <summary>
    /// Clear all references to a box
    /// </summary>
    /// <returns>true if found; true if the removed item had highlights</returns>
    (bool, bool) ForgetAbout(long rowId)
    {
        bool found = false, wasHighlighted = false;
        int i = ScheduledBoxes.FindIndex(r => r.RowId == rowId);
        if (i >= 0)
        {
            wasHighlighted = ScheduledBoxes[i].Visibility == Constants.VISIBILITY_HIGHLIGHT;
            ScheduledBoxes.RemoveAt(i);
            found = true;
        }
        else
        {
            i = TopNotes.FindIndex(r => r.RowId == rowId);
            if (i >= 0)
            {
                TopNotes.RemoveAt(i);
                found = true;
            }
        }
        for (int j = Agenda.Count - 1; j >= 0; --j)
            if (Agenda[j].Box.RowId == rowId)
            {
                Agenda.RemoveAt(j);
                found = true;
            }

        return (found, wasHighlighted);
    }

    /// <summary>
    /// Assign highlight colors and end times to highlighted tasks, to minimize time overlaps of the same color
    /// </summary>
    void AssignHighlights()
    {
        var boxes = Agenda.Where(ae => ae.Box.Visibility == Constants.VISIBILITY_HIGHLIGHT).OrderBy(ae => ae.Box.BoxTime).ToArray();
        var previousEnds = new string[Constants.NHIGHLIGHT_COLORS]; //YYYYMMDDHHMM of when each color was last used until
        Array.Fill(previousEnds, "197012312359");
        int lastAssignedColorNo = -1;
        foreach (var box in boxes)
        {
            box.HighlightEndTime = DateUtil.AddDuration(box.Time, box.Box.Duration);

            //already highlighted, may keep same color (but reject if it overlaps)
            if (box.HighlightColor >= 0)
            {
                if (DateUtil.IsBefore(box.Time, previousEnds[box.HighlightColor])) box.HighlightColor = -1;
            }

            //if unassigned, choose next color that doesn't overlap
            if (box.HighlightColor < 0)
            {
                for (int colorNo = 0; colorNo < Constants.NHIGHLIGHT_COLORS; ++colorNo)
                {
                    if (DateUtil.IsBefore(previousEnds[colorNo], box.Time))
                    {
                        box.HighlightColor = colorNo;
                        lastAssignedColorNo = colorNo;
                        break;
                    }
                }
            }

            //if no possible color found, just cycle through them
            if (box.HighlightColor < 0)
            {
                lastAssignedColorNo = (lastAssignedColorNo + 1) % Constants.NHIGHLIGHT_COLORS;
                box.HighlightColor = lastAssignedColorNo;
            }

            previousEnds[box.HighlightColor] = box.HighlightEndTime;
        }
    }
}
