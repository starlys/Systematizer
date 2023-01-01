namespace Systematizer.WPF;

class BoxSearchController : ListBlockController
{
    /// <summary>
    /// Data container to store box info while block is collapsed
    /// </summary>
    public class CollapseData : ICollapseBlockData
    {
        public bool DoneMode, IncludeDetailsCri;
        public string DoneSinceCri, TermCri;
    }

    public BoxSearchVM VM { get; private set; }

    public override BaseBlockVM GenericVM => VM;

    long LastFocusedBoxId { get; set; } = -1; 

    public BoxSearchController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested, bool doneMode)
        : base(blockGotFocusHandler, collapseRequested)
    {
        VM = new BoxSearchVM(VMGotFocus, doneMode, HandleCommand, SearchRequested);
    }

    public override bool ChangeMode(Mode mode, bool saveChanges)
    {
        return true;
    }

    public override bool HandleCommand(CommandCenter.Item command)
    {
        if (command == Globals.Commands.OPEN)
        {
            if (VM.Results.Count == 0)
            {
                SearchRequested();
                return true;
            }
            if (LastFocusedBoxId < 0) return false;
            var ebox = UIService.LoadBoxForEditing(LastFocusedBoxId);
            if (ebox == null) { UIGlobals.Do.ShowTimedMessge("Cannot find task"); return true; }
            UIGlobals.Do.AddBoxToEditStack(ebox);
            return true;
        }
        if (command == Globals.Commands.IMPORTEXPORT)
        {
            var ids = VM.Results.Select(r => r.Persistent.Box.RowId).ToArray();
            if (ids.Length == 0) return false;
            ExportHtmlDialog.ShowExportDialog(ids, null);
            return true;
        }
        return false;
    }

    public override void Refresh(BoxEditingPool.Item changes)
    {
    }

    public void SearchRequested()
    {
        VisualUtils.LoseRegainFocus();
        string doneSince = null;
        if (VM.DoneMode)
        {
            doneSince = VM.DoneSinceCri.Date;
            if (doneSince == null) return; //must have done date in done mode
        }
        var cachedBoxes = UIService.LoadBoxesByKeyword(VM.TermCri, VM.IncludeDetailsCri, doneSince);
        VM.Results.Clear();
        if (cachedBoxes == null) return;
        foreach (var cb in cachedBoxes)
            VM.Results.Add(new BoxPreviewVM(new AgendaEntry { Box = cb, Time = cb.BoxTime }, null, ItemGotFocus));
        foreach (var vm in VM.Results) vm.TimeClicked = HandleTimeClicked;

        var searchBtn = VM.GetPreResultsControl?.Invoke();
        if (searchBtn != null && cachedBoxes.Length > 0)
        {
            VisualUtils.DelayThen(20, () =>
            {
                searchBtn.Focus();
                searchBtn.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
            });
        }
    }

    void HandleTimeClicked(BoxPreviewVM pvm, FrameworkElement eventSource)
    {
        LastFocusedBoxId = pvm.Persistent.Box.RowId;
        UIGlobals.Deferred.OnOpenBox = new DeferredBehaviors.OpenBoxBehavior { MakeUndone = true };
        if (VM.DoneMode)
            UIGlobals.Do.ShowTimedMessge("Completed task was opened, so it is no longer marked as done!");
        HandleCommand(Globals.Commands.OPEN);
    }

    void ItemGotFocus(BoxPreviewVM itemVM)
    {
        LastFocusedBoxId = itemVM.Persistent.Box.RowId;
    }
}
