using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
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
            VM.RequestAddChunk = () =>
            {
                VM.Chunks.Add(new TodayVM.ChunkVM { Title = VM.NewChunkTitle, IsDirty = true });
                VM.NewChunkTitle = "";
            };
            VM.DropOnChunkRequested = (BoxDragInfo di, TodayVM.ChunkVM chunkVM) =>
            {
                MoveBoxToChunk(di.Box, chunkVM, true);
            };
            VM.FocusBarClicked = () =>
            {
                VisualUtils.DelayThen(100, () =>
                {
                    VM.GetMainControl?.Invoke()?.Focus();
                });
            };

            Refresh(null);
        }

        public override void Refresh(BoxEditingPool.Item changes)
        {
            //no refresh needed if we know the reason for calling is a non-scheduled box change
            //(but we still might need to refresh if there was no change)
            if (changes != null && !changes.IsAgendaChanged) return;

            string fourHours = DateUtil.ToYMDHM(DateTime.Now.AddHours(4));

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
            bool wasChanged = hash != priorContentHash;
            priorContentHash = hash;
            if (!wasChanged) return;

            VM.Chunks.Clear();

            if (chunkList != null && chunkList.Chunks.Count > 0)
            {
                //init VM's chunks
                foreach (var c in chunkList.Chunks)
                    VM.Chunks.Add(new TodayVM.ChunkVM
                    {
                        Title = c.Title,
                        Remove = UserRemovedChunk
                    });

                //init VM's boxes within chunks
                foreach (var a in agendaItems)
                {
                    //chunk assignment can come from persistence, from DeferredBehavior, or default to first chunk
                    var assignedChunk = chunkList.Chunks.FirstOrDefault(c => c.BoxIds != null && c.BoxIds.Contains(a.Box.RowId));
                    if (assignedChunk == null)
                    {
                        var ta = UIGlobals.Deferred.GetAndRemoveTaskAssign(a.Box.RowId);
                        if (ta != null) assignedChunk = chunkList.Chunks.FirstOrDefault(c => c.Title == ta.ChunkTitle);
                    }
                    if (assignedChunk == null) assignedChunk = chunkList.Chunks[0];
                    int chindex = chunkList.Chunks.IndexOf(assignedChunk);

                    var preview = new BoxPreviewVM(a, Date, ItemGotFocus);
                    preview.TimeClicked = HandleTimeClicked;
                    preview.DragStartRequested = DragStartRequested;
                    preview.MouseOpenRequested = MouseOpenRequested;
                    preview.DropUnderBoxRequested = DropUnderBox;
                    VM.Chunks[chindex].Items.Add(preview);
                }
            }
        }

        /// <summary>
        /// loose hashing algorithm to detect changes in a set of displayable data
        /// </summary>
        int HashContent(List<AgendaEntry> agendaItems, MultiDayChunkSet.DayChunkSet chunkList)
        {
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
                if (maxtime.Length == 12) box.BoxTime = Date + maxtime.Substring(8, 4);
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

        void DragStartRequested(BoxPreviewVM pvm, FrameworkElement eventSource)
        {
            if (DraggableBoxVM == null) return;
            var dragData = new DataObject(nameof(BoxDragInfo), new BoxDragInfo { Box = DraggableBoxVM.Persistent.Box });
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
                UIGlobals.Do.ShowTimedMessge("Cannot drag task to a different day");
                return;
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
                var ebox = Globals.UI.LoadBoxForEditing(di.Box.RowId);
                ebox.Box.BoxTime = halfTime;
                Globals.UI.SaveBox(ebox, false);
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
                var ebox = Globals.UI.LoadBoxForEditing(LastFocusedBoxId);
                if (ebox == null) { UIGlobals.Do.ShowTimedMessge("Cannot find task"); return true; }
                UIGlobals.Do.AddBoxToEditStack(ebox);
                return true;
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
            Globals.UI.SaveDayChunks();
        }

        void UserRemovedChunk(TodayVM.ChunkVM cvm)
        {
            VM.Chunks.Remove(cvm);
            SaveChunks(force: true);
            Refresh(null);
        }
    }
}
