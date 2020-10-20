using System;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class ExtPersonController : BlockController
    {
        /// <summary>
        /// Data container to store box info while block is collapsed
        /// </summary>
        public class CollapseData : ICollapseBlockData
        {
            public long RowId;
        }

        public ExtPersonVM VM { get; private set; }

        public override BaseBlockVM GenericVM => VM;

        long? BoxToLinkOnSave; //see deferred behaviors

        public ExtPersonController(long rowId, Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested, bool editMode)
            : base(blockGotFocusHandler, collapseRequested)
        {
            var ep = Globals.UI.LoadPerson(rowId);
            FinishConstructor(ep, editMode);
        }

        public ExtPersonController(ExtPerson ep, Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested, bool editMode)
            : base(blockGotFocusHandler, collapseRequested)
        {
            FinishConstructor(ep, editMode);
        }

        void FinishConstructor(ExtPerson ep, bool editMode)
        {
            //get label and ensure no blanks
            string customLabel(int idx)
            {
                string s = Globals.PersonCustomLabels[idx];
                if (string.IsNullOrEmpty(s)) return null;
                return s + ": ";
            }

            VM = new ExtPersonVM(ep, VMGotFocus)
            {
                CustomLabel1 = customLabel(0),
                CustomLabel2 = customLabel(1),
                CustomLabel3 = customLabel(2),
                CustomLabel4 = customLabel(3),
                CustomLabel5 = customLabel(4)
            };
            if (editMode) VM.IsEditMode = true;

            //hook up VM events
            VM.FocusBarClicked = () =>
            {
                ChangeMode(Mode.Edit, false);
                VisualUtils.DelayThen(100, () =>
                {
                    VM.GetMainControl?.Invoke()?.Focus();
                });
            };
            VM.LinkClicked = linkItemVM =>
            {
                UIGlobals.Do.OpenBlockFromLink(linkItemVM.Link, linkItemVM.OtherId);
            };

            //handle deferred behaviors
            if (UIGlobals.Deferred.OnNewPerson != null)
            {
                BoxToLinkOnSave = UIGlobals.Deferred.OnNewPerson.LinkedBoxId;
                UIGlobals.Deferred.OnNewPerson = null;
            }

            //edit mode only for new record
            VM.IsEditMode = editMode || ep.Person.RowId == 0;
        }

        public void ReloadLinks()
        {
            Globals.UI.UpdateLinks(VM.Persistent);
            VM.InitializeLinksFromPersistent();
            VM.Links.Touch();
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
            VisualUtils.LoseRegainFocus();
            bool failedSave = false;
            if (saveChanges && VM.IsDirty)
            {
                VM.WriteToPersistent();
                try
                {
                    Globals.UI.SavePerson(VM.Persistent);
                    if (BoxToLinkOnSave != null)
                    {
                        Globals.UI.WritePersonLink(new LinkInstruction
                        {
                            Link = LinkType.FromBoxToPerson,
                            FromId = BoxToLinkOnSave.Value,
                            ToId = VM.Persistent.Person.RowId
                        });
                        BoxToLinkOnSave = null;
                    }
                }
                catch (Exception ex)
                {
                    failedSave = true;
                    UIGlobals.Do.ShowTimedMessge("Cannot save: " + ex.Message);
                }
                VM.IsDirty = false;
            }
            VM.IsEditMode = mode == Mode.Edit || failedSave;
            return !failedSave;
        }

        public override bool HandleCommand(CommandCenter.Item command)
        {
            if (command == Globals.Commands.OPEN)
            {
                VM.IsEditMode = true;
                return true;
            }
            if (command == Globals.Commands.ABANDON)
            {
                Globals.UI.AbandonBox(VM.Persistent.Person.RowId);
                CollapseRequested(this, VM.Persistent.Person.RowId == 0);
                UIGlobals.Do.ShowTimedMessge("Edits rolled back");
                return true;
            }
            if (command == Globals.Commands.ENDEDITS)
            {
                if (VM.IsEditMode)
                {
                    ChangeMode(Mode.ReadOnly, true);
                    return true;
                }
                return false; //ancestor will collapse block
            }
            if (command == Globals.Commands.CLOSE)
            {
                ChangeMode(Mode.ReadOnly, true);
                CollapseRequested(this, false);
                return true;
            }
            if (command == Globals.Commands.NEWLINKEDBOX)
            {
                //if not saved, save to get parent id
                if (VM.Persistent.Person.RowId == 0)
                {
                    if (!ChangeMode(Mode.Edit, true)) return true;
                }
                UIGlobals.Deferred.OnNewBox = new DeferredBehaviors.NewBoxBehavior { LinkedPersonId = VM.Persistent.Person.RowId };
                UIGlobals.Do.HandleGlobalCommand(Globals.Commands.NEWITEM);
                return true;
            }
            if (command == Globals.Commands.EDITLINKS)
            {
                UIGlobals.RecordLinkController.ActivateFor(this);
                return true;
            }
            if (command == Globals.Commands.EDITCATEGORIES)
            {
                ChangeMode(Mode.Edit, false);
                if (CatMultiselectDialog.SelectCats(VM.Persistent))
                {
                    VM.IsDirty = true;
                    VM.InitializeCatsFromPersistent();
                }
                return true;
            }
            if (command == Globals.Commands.SENDEMAIL)
            {
                if (!string.IsNullOrEmpty(VM.MainEmail))
                    VisualUtils.ComposeEmailTo(VM.MainEmail);
                return true;
            }
            if (command == Globals.Commands.DELETEPERSON)
            {
                if (VM.Persistent.Person.RowId == 0)
                    UIGlobals.Do.HandleGlobalCommand(Globals.Commands.ABANDON);
                else
                {
                    if (MessageBox.Show(App.Current.MainWindow, $"Really permanently delete person ({VM.Name})?", "Systematizer", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                    {
                        CollapseRequested(this, true);
                        Globals.UI.DeletePerson(VM.Persistent.Person.RowId);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
