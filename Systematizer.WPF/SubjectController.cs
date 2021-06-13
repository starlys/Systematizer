using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common;
using Systematizer.Common.PersistentModel;

namespace Systematizer.WPF
{
    class SubjectController : ListBlockController
    {
        public SubjectVM VM { get; private set; }

        public override BaseBlockVM GenericVM => VM;

        long LastFocusedBoxId { get; set; } = -1;

        public SubjectController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested)
            : base(blockGotFocusHandler, collapseRequested)
        {
            VM = new SubjectVM(VMGotFocus)
            {
                ItemGotFocus = rowVM =>
                {
                    LastFocusedBoxId = rowVM.Persistent?.RowId ?? -1;
                },
                ItemExpanded = (rowVM, isExpanded) =>
                {
                    if (isExpanded)
                    {
                        LastFocusedBoxId = rowVM.Persistent.RowId;
                        if (rowVM.Status == SubjectVM.ChildrenStatus.No)
                            HandleCommand(Globals.Commands.OPEN);
                        else if (rowVM.Status == SubjectVM.ChildrenStatus.YesPlaceholder)
                            LoadChildrenOf(rowVM);
                    }
                    else
                    {
                        UnloadChildrenOf(rowVM);
                    }
                },
                OpenRequested = () =>
                {
                    HandleCommand(Globals.Commands.OPEN);
                }
            };

            Refresh(null);
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
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
                UIGlobals.Deferred.OnNewBox = new DeferredBehaviors.NewBoxBehavior { ParentId = LastFocusedBoxId };
                //UIGlobals.Do.HandleGlobalCommand(Globals.Commands.NEWITEM);
                var box = new Box
                {
                    TimeType = Constants.TIMETYPE_NONE,
                    Importance = Constants.IMPORTANCE_NORMAL,
                    Visibility = Constants.VISIBILITY_NORMAL
                };
                var ebox = new ExtBox(box, null);
                UIGlobals.Do.AddBoxToEditStack(ebox);
                UIGlobals.Do.ShowTimedMessge($"New note will be filed under selected note.");
            }
            return base.HandleCommand(command);
        }

        public override void Refresh(BoxEditingPool.Item changes)
        {
            bool rootChanged = changes != null && changes.IsRootSubjectsChanged;
            if (VM.RootRows.Count == 0 || rootChanged)
                RefreshRoot();
            if (changes != null)
                RefreshChildren(changes);
        }

        void RefreshRoot()
        { 
            //collect items from cache 
            var cachedRoots = Globals.BoxCache.GetTopNotes().ToArray();

            //hold on to old row VMs for reuse
            var oldRowVMs = VM.RootRows.ToArray();
            VM.RootRows.Clear();
            var newRows = new List<SubjectVM.RowVM>();

            //quit if empty
            if (cachedRoots.Length == 0) return;

            //rebuild list in current order using old row VMs
            foreach (var cachedBox in cachedRoots)
            {
                var rootVM = oldRowVMs.FirstOrDefault(r => r.Persistent.RowId == cachedBox.RowId);
                if (rootVM == null || rootVM.Persistent != cachedBox) 
                    rootVM = new SubjectVM.RowVM(cachedBox, null);
                newRows.Add(rootVM);
                rootVM.Touch();
            }
            VM.RootRows.AddRange(newRows);

            //handle placeholders under those nodes that have actual children
            var rootIds = cachedRoots.Select(r => r.RowId).ToArray();
            var withChildren = Globals.UI.BoxesWithChildren(rootIds, true);
            foreach (var vm in VM.RootRows)
            {
                bool hasChildren = withChildren.Contains(vm.Persistent.RowId);

                if (vm.Status == SubjectVM.ChildrenStatus.No && hasChildren)
                    vm.Status = SubjectVM.ChildrenStatus.YesPlaceholder;
                else if (!hasChildren)
                    vm.Status = SubjectVM.ChildrenStatus.No;
            }
        }

        /// <summary>
        /// Refresh all expanded non-root nodes that could be affected by the given change (more expensive since it loads the nodes)
        /// </summary>
        /// <param name="changes">NOT null</param>
        void RefreshChildren(BoxEditingPool.Item changes)
        {
            foreach (var rowVM in VM.RootRows)
                if (RefreshChildrenOf(rowVM, changes)) break;
        }

        /// <summary>
        /// Recursively refresh rowVM's children if it contains the given box; return true if this or any descendant level
        /// found the box
        /// </summary>
        /// <returns></returns>
        bool RefreshChildrenOf(SubjectVM.RowVM rowVM, BoxEditingPool.Item changes)
        {
            long thisId = rowVM.Persistent.RowId;
            bool reloadNeeded = false;
            if (rowVM.Status == SubjectVM.ChildrenStatus.No)
                reloadNeeded = changes.NewParentId == thisId;
            else if (rowVM.Status == SubjectVM.ChildrenStatus.YesLoaded)
                reloadNeeded = (changes.IsParentageChanged || changes.IsTitleChanged || changes.NewDoneDate != changes.OldDoneDate) 
                    && (changes.OldParentId == thisId || changes.NewParentId == thisId);
            else if (rowVM.Status == SubjectVM.ChildrenStatus.YesPlaceholder)
                reloadNeeded = false; //was: changes.IsParentageChanged && (changes.OldParentId == thisId || changes.NewParentId == thisId);

            //found it, so reload (which collapses children)
            if (reloadNeeded)
            {
                LoadChildrenOf(rowVM);
                return true;
            }

            //didn't find it, so recurse children and exit when found
            if (rowVM.Status == SubjectVM.ChildrenStatus.YesLoaded)
                foreach (var c in rowVM.Children)
                    if (RefreshChildrenOf(c, changes))
                        return true;
            
            return false;
        }

        /// <summary>
        /// Load children and return true if any found
        /// </summary>
        static bool LoadChildrenOf(SubjectVM.RowVM parent)
        {
            var children = Globals.UI.LoadBoxesByParent(parent.Persistent.RowId, true);
            parent.Status = children.Any() ? SubjectVM.ChildrenStatus.YesLoaded : SubjectVM.ChildrenStatus.No;
            parent.Children.AddRange(children.Select(r => new SubjectVM.RowVM(r, parent)));

            var ids = children.Select(r => r.RowId).ToArray();
            var withChildren = Globals.UI.BoxesWithChildren(ids, true);
            foreach (var id in withChildren)
            {
                var child = parent.Children.FirstOrDefault(r => r.Persistent.RowId == id);
                if (child != null) child.Status = SubjectVM.ChildrenStatus.YesPlaceholder;
            }

            return parent.Status == SubjectVM.ChildrenStatus.YesLoaded;
        }

        static void UnloadChildrenOf(SubjectVM.RowVM parent)
        {
            if (parent.Status == SubjectVM.ChildrenStatus.YesLoaded)
                parent.Status = SubjectVM.ChildrenStatus.YesPlaceholder;
        }
    }
}
