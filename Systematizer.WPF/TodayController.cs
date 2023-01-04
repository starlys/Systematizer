using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Systematizer.WPF;

class TodayController : ListBlockController
{
    /// <summary>
    /// Data container to store box info while block is collapsed
    /// </summary>
    public class CollapseData : ICollapseBlockData
    {
        public bool IsToday;
    }

    public bool IsToday { get; private set; } //false means is tomorrow
    int priorContentHash;
    public TodayVM VM { get; private set; }
    BoxPreviewVM DraggableBoxVM;
    long LastFocusedBoxId { get; set; } = -1;
    TodayVM.ChunkVM LastFocusedChunk; //only tracks if focus in chunk title, then cleared if focus goes to a task

    /// <summary>
    /// YYYYMMDD for date showing
    /// </summary>
    public string Date { get; private set; }

    public override BaseBlockVM GenericVM => VM;

    public TodayController(bool isToday, Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested) 
        : base(blockGotFocusHandler, collapseRequested)
    {
        IsToday = isToday;
        DateTime dt = DateTime.Now;
        if (!isToday) dt = dt.AddDays(1);
        string dateS = DateUtil.ToYMD(dt);
        Date = dateS;
        VM = new TodayVM(isToday, dateS, VMGotFocus);

        //hook up VM events
        VM.ChunkGotFocus = idx =>
        {
            LastFocusedChunk = VM.Chunks[idx];
        };
        VM.ChunkLostFocus = idx =>
        {
            if (idx < 0 || idx >= VM.Chunks.Count) return;
            var chunkVM = VM.Chunks[idx];

            //on delete chunk title, delete the chunk; focus next only if there are others
            //(note that an attempt to focus could be circular if we try to focus the same thing that is losing focus)
            if (string.IsNullOrEmpty(chunkVM.Title))
            {
                bool wasRemoved = UserRemovedChunk(chunkVM);
                if (wasRemoved)
                    VisualUtils.DelayThen(10, () => VM.GetMainControl()?.Focus());
            }
        };
        VM.RequestAddChunk = () =>
        {
            VM.Chunks.Add(new TodayVM.ChunkVM()
            {
                Title = VM.NewChunkTitle,
                IsDirty = true
            });
            VM.NewChunkTitle = "";
        };
        VM.DropBoxOnChunkRequested = (BoxDragInfo di, TodayVM.ChunkVM chunkVM) =>
        {
            MoveBoxToChunk(di.Box, chunkVM, true);
        };
        VM.DropChunkOnChunkRequested = (ChunkDragInfo di, TodayVM.ChunkVM toChunkVM) =>
        {
            ResequenceChunks(di.FromIndex, toChunkVM);
        };
        VM.FocusBarClicked = () =>
        {
            VisualUtils.DelayThen(30, () =>
            {
                VM.GetMainControl?.Invoke()?.Focus();
            });
        };
        VM.ChunkDragStartRequested = ChunkDragStartRequested;

        Refresh(null);
    }

    public override void Refresh(BoxEditingPool.Item changes)
    {
        //no refresh needed if we know the reason for calling is a non-scheduled box change
        //(but we still might need to refresh if there was no change)
        if (changes != null && !changes.IsAgendaChanged) return;

        SaveChunks();

        string fourHours = DateUtil.ToYMDHM(DateTime.Now.AddHours(4));
        bool anyChunkAssignments = false;

        //collect items from cache
        var agenda = Globals.BoxCache.GetAgenda();
        var agendaItems = agenda.Where(r => r.Time.StartsWith(Date)).ToList();
        if (IsToday)
        {
            string midnight = Date + "0000";
            var oldStuff = agenda.Where(r => DateUtil.IsBefore(r.Time, midnight));
            agendaItems.AddRange(oldStuff);
        }

        //disregard low-clutter items if its time is more than 4 hrs in the future
        agendaItems = agendaItems.Where(a =>
        {
            bool shouldHide = a.Box.Visibility == Constants.VISIBILITY_LOWCLUTTER
                && DateUtil.IsBefore(fourHours, a.Box.BoxTime);
            return !shouldHide;
        })
            .OrderBy(a => a.Box.BoxTime)
            .ToList();

        //get list of chunks established for the day
        var chunkList = Globals.DayChunks.Days.FirstOrDefault(c => c.Date == Date);

        //exit now if no changes
        int hash = HashContent(agendaItems, chunkList);
        bool wasChanged = hash != priorContentHash || chunkList == null;
        priorContentHash = hash;
        if (!wasChanged) return;

        VM.Chunks.Clear();

        if (chunkList != null && chunkList.Chunks.Count > 0)
        {
            //init VM's chunks
            foreach (var c in chunkList.Chunks)
                VM.Chunks.Add(new TodayVM.ChunkVM()
                {
                    Title = c.Title
                });

            //init VM's boxes within chunks
            foreach (var a in agendaItems)
            {
                //chunk assignment can come from persistence, from DeferredBehavior, from changes argument, or default to first chunk
                var assignedChunk = chunkList.Chunks.FirstOrDefault(c => c.BoxIds != null && c.BoxIds.Contains(a.Box.RowId));
                if (assignedChunk == null)
                {
                    var ta = UIGlobals.Deferred.GetAndRemoveTaskAssign(a.Box.RowId);
                    if (ta != null)
                    {
                        assignedChunk = chunkList.Chunks.FirstOrDefault(c => c.Title == ta.ChunkTitle);
                        anyChunkAssignments = true;
                    }
                    else if (changes != null && changes.NewBoxId == a.Box.RowId && changes.OldBoxTime == null)
                    {
                        //newly added task should go to the 2nd or 3rd chunk based on time of day
                        int cidx = MultiDayChunkSet.GetDefaultChunkIndex(a.Time);
                        if (cidx == 2 && chunkList.Chunks.Count > 2) assignedChunk = chunkList.Chunks[2];
                        if (cidx == 1 && chunkList.Chunks.Count > 1) assignedChunk = chunkList.Chunks[1];
                        anyChunkAssignments = true;
                    }
                }
                assignedChunk ??= chunkList.Chunks[0];
                int chindex = chunkList.Chunks.IndexOf(assignedChunk);

                var preview = new BoxPreviewVM(a, Date, ItemGotFocus)
                {
                    TimeClicked = HandleTimeClicked,
                    DragStartRequested = BoxDragStartRequested,
                    MouseOpenRequested = MouseOpenRequested,
                    DropUnderBoxRequested = DropUnderBox
                };
                VM.Chunks[chindex].Items.Add(preview);
            }
        }
        if (anyChunkAssignments) SaveChunks(force: true);
    }

    /// <summary>
    /// loose hashing algorithm to detect changes in a set of displayable data
    /// </summary>
    static int HashContent(List<AgendaEntry> agendaItems, MultiDayChunkSet.DayChunkSet chunkList)
    {
        if (chunkList == null || agendaItems == null) return 0;
        unchecked
        {
            int hash = 0;
            foreach (var a in agendaItems) hash += (int)a.Box.RowId + a.Box.Title.GetHashCode() + a.Box.BoxTime.GetHashCode();
            foreach (var c in chunkList.Chunks)
            {
                int titleHash = c.Title.GetHashCode();
                hash += titleHash;
                if (c.BoxIds != null && c.BoxIds.Length > 0) hash += (int)c.BoxIds.Sum(id => id + titleHash);
            }
            hash += chunkList.Date.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Create a new task box whose time and chunk assignment are based on the current selection
    /// </summary>
    void NewTaskNearSelection()
    {
        var chunkVM = GetActiveChunk();
        if (chunkVM == null) return;

        //create task box defaulted to the same time as the latest item in this chunk
        var box = BoxCreator.GetPreset(BoxCreator.TASK_PRESET_NO);
        box.BoxTime = Date + "0900";
        if (chunkVM.Items.Any())
        {
            string maxtime = chunkVM.Items.Max(b => b.Persistent.Box.BoxTime);
            if (maxtime.Length == 12) box.BoxTime = string.Concat(Date, maxtime.AsSpan(8, 4));
        }

        var ebox = new ExtBox(box, null);
        UIGlobals.Do.AddBoxToEditStack(ebox);

        //keep the chunk associated with the box, so after it is saved, it shows in the right chunk
        UIGlobals.Deferred.TaskAssigns.Add(new DeferredBehaviors.TaskAssignBehavior
        {
            Box = box,
            ChunkTitle = chunkVM.Title
        });
    }

    /// <summary>
    /// Get the chunk that is focused or the chunk containing the last focused box, or null if there is no focus
    /// </summary>
    TodayVM.ChunkVM GetActiveChunk()
    {
        if (LastFocusedChunk != null) return LastFocusedChunk;
        if (LastFocusedBoxId <= 0) return null;

        //find chunk owning focused box
        foreach (var c in VM.Chunks)
            foreach (var b in c.Items)
                if (b.Persistent.Box.RowId == LastFocusedBoxId)
                    return c;
        return null;
    }

    void HandleTimeClicked(BoxPreviewVM pvm, FrameworkElement eventSource)
    {
        LastFocusedChunk = null;
        LastFocusedBoxId = pvm.Persistent.Box.RowId;
        DraggableBoxVM = pvm;
    }

    void BoxDragStartRequested(BoxPreviewVM pvm, FrameworkElement eventSource)
    {
        if (DraggableBoxVM == null) return;
        var dragData = new DataObject(nameof(BoxDragInfo), new BoxDragInfo { Box = DraggableBoxVM.Persistent.Box });
        DragDrop.DoDragDrop(eventSource, dragData, DragDropEffects.Move);
    }

    void ChunkDragStartRequested(int chunkIdx, FrameworkElement eventSource)
    {
        var dragData = new DataObject(nameof(ChunkDragInfo), new ChunkDragInfo { VM = VM, FromIndex = chunkIdx });
        DragDrop.DoDragDrop(eventSource, dragData, DragDropEffects.Move);
    }

    //handler for result of drag onto another boxpreview
    void DropUnderBox(BoxDragInfo di, BoxPreviewVM target)
    {
        //allowed?
        if (di.Box.TimeType >= Constants.TIMETYPE_MINUTE)
        {
            UIGlobals.Do.ShowTimedMessge("Cannot drag tasks scheduled for an exact time; instead, open the task and change the time");
            return;
        }
        if (!di.Box.BoxTime.StartsWith(Date))
        {
            if (!VM.ContainsBoxId(di.Box.RowId))
            {
                UIGlobals.Do.ShowTimedMessge("Cannot drag task to a different day");
                return;
            }
        }

        //get target chunk and position of this target previewbox in that chunk
        TodayVM.ChunkVM targetChunk = null;
        int indexInTarget = -1;
        foreach (var ch in VM.Chunks)
        {
            for (int i = 0; i < ch.Items.Count; ++i)
            {
                if (ReferenceEquals(ch.Items[i].Persistent.Box, target.Persistent.Box))
                {
                    targetChunk = ch;
                    indexInTarget = i;
                    goto escape1;
                }
            }
        }
        escape1:
        if (targetChunk == null) //should not happen
        {
            UIGlobals.Do.ShowTimedMessge("Cannot drag task: unexpected error");
            return;
        }

        //what time is represented by the drop location?
        var timesInChunk = targetChunk.Items.Select(bp => bp.Persistent.Time).OrderBy(s => s).ToArray();
        string time1 = target.Persistent.Box.BoxTime;
        string time2 = Date + "2359";
        if (indexInTarget < targetChunk.Items.Count - 1) time2 = targetChunk.Items[indexInTarget + 1].Persistent.Box.BoxTime;
        string halfTime = DateUtil.HalfWayBetween(time1, time2);

        //change time and save box
        try
        {
            var ebox = UIService.LoadBoxForEditing(di.Box.RowId);
            ebox.Box.BoxTime = halfTime;
            UIService.SaveBox(ebox, false);
        }
        catch (Exception ex)
        {
            UIGlobals.Do.ShowTimedMessge("Could not move: " + ex.Message);
            return;
        }

        //inherit chunk from target and rebuild views
        MoveBoxToChunk(di.Box, targetChunk, true);
    }

    /// <summary>
    /// handler for result of a drag onto a chunk title, or onto another boxpreview (indirectly).
    /// </summary>
    void MoveBoxToChunk(CachedBox box, TodayVM.ChunkVM chunk, bool rebulidViews)
    {
        if (!VM.ContainsBoxId(box.RowId))
        {
            UIGlobals.Do.ShowTimedMessge("Cannot drag task to a different day");
            return;
        }

        //remove box from all chunks
        foreach (var c in VM.Chunks)
        {
            for (int i = c.Items.Count - 1; i >= 0; --i)
                if (c.Items[i].Persistent.Box.RowId == box.RowId)
                {
                    c.Items.RemoveAt(i);
                    c.IsDirty = true;
                }
        }

        //add to target chunk
        var ae = new AgendaEntry { Box = box }; //this is short lived since we will rebuild today anyway 
        chunk.Items.Add(new BoxPreviewVM(ae, Date, ItemGotFocus));
        chunk.IsDirty = true;
        SaveChunks(force: true);

        //this handler can only affect today so we only refresh this one view
        if (rebulidViews) Refresh(null);
    }

    /// <summary>
    /// handler for result of a drag of a chunk onto a chunk title
    /// </summary>
    void ResequenceChunks(int fromChunkIdx, TodayVM.ChunkVM dropOnChunk)
    {
        if (fromChunkIdx < 0 || fromChunkIdx >= VM.Chunks.Count) return;
        int toChunkIdx = VM.Chunks.IndexOf(dropOnChunk);
        if (toChunkIdx < 0 || toChunkIdx >= VM.Chunks.Count 
            || toChunkIdx == fromChunkIdx || toChunkIdx == fromChunkIdx - 1) return;
        var draggedChunk = VM.Chunks[fromChunkIdx];
        VM.Chunks.RemoveAt(fromChunkIdx);
        int insertAt = toChunkIdx < fromChunkIdx ? toChunkIdx + 1 : toChunkIdx; 
        if (insertAt < 0) return;
        if (insertAt >= VM.Chunks.Count) VM.Chunks.Add(draggedChunk);
        else VM.Chunks.Insert(insertAt, draggedChunk);
        Refresh(null);
    }

    void MouseOpenRequested(BoxPreviewVM pvm)
    {
        HandleCommand(Globals.Commands.OPEN);
    }

    public override bool ChangeMode(Mode mode, bool saveChanges)
    {
        if (saveChanges) SaveChunks();
        return true; 
    }

    public override bool HandleCommand(CommandCenter.Item command)
    {
        if (command == Globals.Commands.OPEN) 
        {
            if (LastFocusedBoxId < 0) return false;
            var ebox = UIService.LoadBoxForEditing(LastFocusedBoxId);
            if (ebox == null) { UIGlobals.Do.ShowTimedMessge("Cannot find task"); return true; }
            UIGlobals.Do.AddBoxToEditStack(ebox);
            return true;
        }
        if (command == Globals.Commands.CLOSE)
        {
            VisualUtils.LoseRegainFocus();
            SaveChunks();
            return false; //caller can handle collapse
        }
        if (command == Globals.Commands.NEWLINKEDBOX)
        {
            NewTaskNearSelection();
        }
        return false;
    }

    void ItemGotFocus(BoxPreviewVM itemVM)
    {
        LastFocusedChunk = null;
        LastFocusedBoxId = itemVM.Persistent.Box.RowId;
        DraggableBoxVM = itemVM;
    }

    /// <summary>
    /// Save current day-chunk arrangement if there are changes
    /// </summary>
    public void SaveChunks(bool force = false)
    {
        if (!force && !VM.Chunks.Any(c => c.IsDirty)) return;
        var gday = Globals.DayChunks.Days.FirstOrDefault(d => d.Date == Date);
        if (gday == null) { gday = new MultiDayChunkSet.DayChunkSet { Date = Date }; Globals.DayChunks.Days.Add(gday); }
        bool isFirst = true;
        gday.Chunks.Clear();
        foreach (var c in VM.Chunks)
        {
            c.IsDirty = false;
            long[] boxIds = null;
            if (!isFirst) boxIds = c.Items.Select(i => i.Persistent.Box.RowId).ToArray();
            gday.Chunks.Add(new MultiDayChunkSet.Chunk { Title = c.Title, BoxIds = boxIds });
            isFirst = false;
        }
        UIService.SaveDayChunks();
    }

    /// <summary>
    /// True if could be removed
    /// </summary>
    bool UserRemovedChunk(TodayVM.ChunkVM cvm)
    {
        //can't remove last one
        if (VM.Chunks.Count <= 1) return false;

        VM.Chunks.Remove(cvm);
        SaveChunks(force: true);
        Refresh(null);
        return true;
    }
}
