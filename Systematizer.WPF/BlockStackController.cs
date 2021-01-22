using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// controller for block stacks - there are only 2 of these instances owned by MainController
    /// </summary>
    class BlockStackController : BaseController
    {
        readonly WholeVM.Stack VM;
        readonly List<BaseController> Controllers = new List<BaseController>(); //parallel list with VM.Blocks
        BlockController FocusedChild; //null if none; still set even if stack does not have focus
        bool StackHasFocus;
        readonly Action StackGotFocus;

        public BlockStackController(WholeVM.Stack vm, Action gotFocusAction)
        {
            VM = vm;
            StackGotFocus = gotFocusAction;
        }

        public void InitializeHomeStack()
        {
            Clear();
            if (!Globals.AllowTasks) return;

            //done
            var done = CollapsedBlockController.CreateControllerForDoneBlock(ChildGotFocus, ReinflateCollapsedBlock);
            Controllers.Add(done);
            VM.Blocks.Add(done.VM);

            //today/tomorrow
            var today = new TodayController(true, ChildGotFocus, ReplaceBlockWithCollapsed);
            Controllers.Add(today);
            VM.Blocks.Add(today.VM);
            var tomorrow = new TodayController(false, ChildGotFocus, ReplaceBlockWithCollapsed);
            var collapsedTomorrow = CollapsedBlockController.CreateControllerFor(tomorrow.VM, ChildGotFocus, ReinflateCollapsedBlock);
            Controllers.Add(collapsedTomorrow);
            VM.Blocks.Add(collapsedTomorrow.VM);

            //agenda and calendar
            var agenda = new AgendaController(ChildGotFocus, ReplaceBlockWithCollapsed, 1);
            var collapsedAgenda = CollapsedBlockController.CreateControllerFor(agenda.VM, ChildGotFocus, ReinflateCollapsedBlock);
            Controllers.Add(collapsedAgenda);
            VM.Blocks.Add(collapsedAgenda.VM);
            var cal = new CalendarController(ChildGotFocus, ReplaceBlockWithCollapsed);
            var collapsedCal = CollapsedBlockController.CreateControllerFor(cal.VM, ChildGotFocus, ReinflateCollapsedBlock);
            Controllers.Add(collapsedCal);
            VM.Blocks.Add(collapsedCal.VM);

            //subjects
            var subjects = new SubjectController(ChildGotFocus, ReplaceBlockWithCollapsed);
            var collapsedSubjects = CollapsedBlockController.CreateControllerFor(subjects.VM, ChildGotFocus, ReinflateCollapsedBlock);
            Controllers.Add(collapsedSubjects);
            VM.Blocks.Add(collapsedSubjects.VM);

            FocusByIndex(1); //today
        }

        public void InitializeEditStack()
        {
            //load all unclassifieds
            Clear();
            var boxes = Globals.UI.LoadUnclassBoxesForEditing();
            foreach (var ebox in boxes) Add(ebox, false, true);
        }

        public override bool HandleCommand(CommandCenter.Item command)
        {
            bool wasHandled = FocusedChild?.HandleCommand(command) == true;
            if (wasHandled) return true;

            if (command == Globals.Commands.CLOSE)
            {
                //collapse block if child handler didn't handle it
                if (FocusedChild != null)
                    ReplaceBlockWithCollapsed(FocusedChild, false);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            Controllers.Clear();
            VM.Blocks.Clear();
        }

        /// <summary>
        /// Refresh all ListBlockControllers in the stack
        /// </summary>
        /// <param name="changes">can be null</param>
        public void RefreshAllLists(BoxEditingPool.Item changes)
        {
            foreach (var c in Controllers.OfType<ListBlockController>()) c.Refresh(changes);
        }

        /// <summary>
        /// Get controllers that match given type
        /// </summary>
        public IEnumerable<BlockController> GetByType<T>() where T:BlockController
        {
            return Controllers.OfType<T>();
        }

        public void LoseFocus()
        {
            FocusedChild?.LoseFocus();
            StackHasFocus = false;
        }

        void ChildGotFocus(BlockController c)
        {
            if (c != FocusedChild || !StackHasFocus)
            {
                FocusedChild?.LoseFocus();
                FocusedChild = c;
                c.GenericVM.HasBlockFocus = true;
            }
            if (!StackHasFocus)
            {
                StackHasFocus = true;
                StackGotFocus();
            }
        }

        /// <summary>
        /// Add any controller/VM to top of stack and focus it
        /// </summary>
        /// <param name="creatorFunc">function accepting handler for block getting focus, and handler for collapsing block, in that order</param>
        public void AddToTop(Func<Action<BlockController>, Action<BlockController, bool>, BlockController> creatorFunc)
        {
            var c = creatorFunc(ChildGotFocus, ReplaceBlockWithCollapsed);
            Controllers.Insert(0, c);
            VM.Blocks.Insert(0, c.GenericVM);
            FocusByIndex(0);
        }

        /// <summary>
        /// Add box to stack
        /// </summary>
        public void Add(ExtBox ebox, bool focus, bool editMode)
        {
            //attempt to shortcircuit by finding the same box which is already open or collapsed
            if (ebox.Box.RowId != 0)
            {
                var existing = Controllers.OfType<ExtBoxController>().FirstOrDefault(c => c.VM.Persistent.Box.RowId == ebox.Box.RowId);
                if (existing != null)
                {
                    int stackindex = Controllers.IndexOf(existing);
                    FocusByIndex(stackindex);
                    return;
                }
                var existingCollapsed = Controllers.OfType<CollapsedBlockController>()
                    .FirstOrDefault(c => c.OriginalControllerType == typeof(ExtBoxController) 
                    && ((ExtBoxController.CollapseData)(c.Data)).RowId == ebox.Box.RowId);
                if (existingCollapsed != null)
                {
                    ReinflateCollapsedBlock(existingCollapsed);
                    return;
                }
            }

            var c = new ExtBoxController(ebox, ChildGotFocus, ReplaceBlockWithCollapsed, editMode);
            VM.Blocks.Insert(0, c.GenericVM);
            Controllers.Insert(0, c);
            if (focus) FocusByIndex(0);
            UIGlobals.Do.UserActionCompleted(false);
        }

        /// <summary>
        /// Add person to stack
        /// </summary>
        public void Add(ExtPerson ep, bool focus, bool editMode)
        {
            //attempt to shortcircuit by finding the same box which is already open or collapsed
            if (ep.Person.RowId != 0)
            {
                var existing = Controllers.OfType<ExtPersonController>().FirstOrDefault(c => c.VM.Persistent.Person.RowId == ep.Person.RowId);
                if (existing != null)
                {
                    int stackindex = Controllers.IndexOf(existing);
                    FocusByIndex(stackindex);
                    return;
                }
                var existingCollapsed = Controllers.OfType<CollapsedBlockController>()
                    .FirstOrDefault(c => c.OriginalControllerType == typeof(ExtPersonController)
                    && ((ExtPersonController.CollapseData)(c.Data)).RowId == ep.Person.RowId);
                if (existingCollapsed != null)
                {
                    ReinflateCollapsedBlock(existingCollapsed);
                    return;
                }
            }

            var c = new ExtPersonController(ep, ChildGotFocus, ReplaceBlockWithCollapsed, editMode);
            VM.Blocks.Insert(0, c.GenericVM);
            Controllers.Insert(0, c);
            if (focus) FocusByIndex(0);
            UIGlobals.Do.UserActionCompleted(false);
        }

        /// <summary>
        /// All programmatic focusing should go through this method
        /// </summary>
        void FocusByIndex(int i)
        {
            VisualUtils.DelayThen(10, () =>
            {
                //there is a chance the index changes since this was called, so re-check
                if (i >= VM.Blocks.Count || i < 0) return;
                var el = VM.Blocks[i]?.GetMainControl?.Invoke();
                if (el != null) el.Focus();
            });
        }

        public void FocusFirstUncollapsed()
        {
            for (int i = 0; i < Controllers.Count; ++i)
            {
                if (Controllers[i] is CollapsedBlockController) continue;
                FocusByIndex(i);
                return;
            }
        }

        /// <summary>
        /// Focus the next (+1) or previous (-1) block in the stack; ensures something gets focus if possible
        /// </summary>
        public void FocusDelta(int delta)
        {
            if (Controllers.Count == 0) return;
            int oldIdx = Controllers.IndexOf(FocusedChild); //could be -1

            //delta 0 case might need to be implemented as delta = 1
            if (delta == 0)
            {
                if (oldIdx < 0 || oldIdx >= Controllers.Count || (Controllers[oldIdx] is CollapsedBlockController))
                    delta = 1;
            }

            oldIdx = Math.Max(oldIdx, 0);
            int newIdx = oldIdx;
            for (int iter = 0; iter < 50; ++iter) //infinite loop control
            {
                newIdx = (newIdx + delta + Controllers.Count) % Controllers.Count;
                if (newIdx == oldIdx || delta == 0) break; //wrapped around
                if (Controllers[newIdx] is CollapsedBlockController) continue;
                break;
            }
            FocusByIndex(newIdx);
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
            bool anyFails = false;
            foreach (var c in Controllers)
                if (!c.ChangeMode(mode, saveChanges))
                    anyFails = true;
            return !anyFails;
        }

        /// <summary>
        /// Remove any collapsed blocks from stack if they are 10 min old
        /// </summary>
        public void RemoveOldCollapsed()
        {
            for (int i = Controllers.Count - 1; i >= 0; --i)
            {
                if (Controllers[i] is CollapsedBlockController cbc)
                {
                    if (cbc.IsExpired())
                    {
                        VM.Blocks.RemoveAt(i);
                        Controllers.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a block from the stack and replace it with a collapsed block 
        /// (Checks cases where not allowed and does nothing if so)
        /// </summary>
        void ReplaceBlockWithCollapsed(BlockController c, bool removeCompletely)
        {
            UIGlobals.RecordLinkController.VM.IsActive = false;
            int stackIndex = Controllers.IndexOf(c);
            if (stackIndex < 0) return;
            var forVM = VM.Blocks[stackIndex];
            var cbc = CollapsedBlockController.CreateControllerFor(forVM, ChildGotFocus, ReinflateCollapsedBlock);
            VM.Blocks.RemoveAt(stackIndex);
            Controllers.RemoveAt(stackIndex);
            if (cbc != null && !removeCompletely)
            {
                VM.Blocks.Insert(stackIndex, cbc.VM);
                Controllers.Insert(stackIndex, cbc);
            }
            SetAgendaDaysToOmit();
            UIGlobals.Do.FocusTopBlock();
            UIGlobals.Do.UserActionCompleted(c is ListBlockController);
        }

        /// <summary>
        /// Remove collapsed block from the stack and replace it with the real block
        /// </summary>
        void ReinflateCollapsedBlock(CollapsedBlockController c)
        {
            int stackIndex = Controllers.IndexOf(c);
            if (stackIndex < 0) return;
            var controller = c.Reinflate(ChildGotFocus, ReplaceBlockWithCollapsed);
            VM.Blocks.RemoveAt(stackIndex);
            Controllers.RemoveAt(stackIndex);
            var vm2 = controller.GenericVM;
            if (vm2 == null) return;
            //if (vm2 is BaseEditableBlockVM vm3) vm3.IsEditMode = true;
            VM.Blocks.Insert(stackIndex, vm2);
            Controllers.Insert(stackIndex, controller);
            FocusByIndex(stackIndex);
            SetAgendaDaysToOmit();
            UIGlobals.Do.UserActionCompleted(controller is ListBlockController);
        }

        /// <summary>
        /// Set agenda days to omit based on visibility of today/tomorrow; does not refresh
        /// </summary>
        void SetAgendaDaysToOmit()
        {
            var agenda = Controllers.OfType<AgendaController>().FirstOrDefault();
            if (agenda == null) return;
            bool todayIsVisible = Controllers.OfType<TodayController>().Any(c => c.IsToday);
            bool tomorrowIsVisible = Controllers.OfType<TodayController>().Any(c => !c.IsToday);
            agenda.NDaysToOmit = tomorrowIsVisible ? 2 : (todayIsVisible ? 1 : 0);
        }
    }
}
