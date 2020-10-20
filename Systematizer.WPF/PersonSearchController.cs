using System;
using System.Linq;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class PersonSearchController : ListBlockController
    {
        /// <summary>
        /// Data container to store box info while block is collapsed
        /// </summary>
        public class CollapseData : ICollapseBlockData
        {
            public bool IncludeDetailsCri;
            public string TermCri;
        }

        public PersonSearchVM VM { get; private set; }

        public override BaseBlockVM GenericVM => VM;

        public PersonSearchController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested)
            : base(blockGotFocusHandler, collapseRequested)
        {
            VM = new PersonSearchVM(VMGotFocus, SearchRequested, OpenRequested);
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
            return true;
        }

        public override void Refresh(BoxEditingPool.Item changes)
        {
        }

        public override bool HandleCommand(CommandCenter.Item command)
        {
            if (command == Globals.Commands.OPEN)
            {
                SearchRequested();
                return true;
            }
            if (command == Globals.Commands.IMPORTEXPORT)
            {
                var ids = VM.Results.Select(r => r.PersonId).ToArray();
                if (ids.Length == 0) return false;
                ExportHtmlDialog.ShowExportDialog(null, ids);
                return true;
            }
            return false;
        }

        public void SearchRequested()
        {
            VisualUtils.LoseRegainFocus();
            var persons = Globals.UI.LoadFilteredPersons(VM.TermCri, VM.IncludeDetailsCri, VM.CatIdCri);
            VM.Results.Clear();
            if (persons == null) return;
            foreach (var p in persons)
                VM.Results.Add(new PersonSearchVM.ResultItem { PersonId = p.RowId, Name = p.Name });
            var searchBtn = VM.GetPreResultsControl?.Invoke();
            VisualUtils.DelayThen(20, () =>
            {
                searchBtn.Focus();
                searchBtn.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
            });

        }

        void OpenRequested(int idx)
        {
            var pvm = VM.Results[idx];
            var ep = Globals.UI.LoadPerson(pvm.PersonId);
            if (ep == null) return;
            UIGlobals.Do.AddPersonToEditStack(ep);
        }

    }
}
