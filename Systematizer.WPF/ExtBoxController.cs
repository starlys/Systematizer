using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Systematizer.Common;
using Microsoft.Win32;
using System.IO;

namespace Systematizer.WPF
{
    class ExtBoxController : BlockController
    {
        /// <summary>
        /// Data container to store box info while block is collapsed
        /// </summary>
        public class CollapseData:ICollapseBlockData
        {
            public long RowId;
        }

        public ExtBoxVM VM { get; private set; }

        public override BaseBlockVM GenericVM => VM;

        /// <summary>
        /// set in ctor and used when first saved
        /// </summary>
        long? LinkToPersonIdOnSave;

        public ExtBoxController(long rowId, Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested, bool editMode) 
            : base(blockGotFocusHandler, collapseRequested)
        {
            var ebox = Globals.UI.LoadBoxForEditing(rowId);
            FinishConstructor(ebox, editMode);
        }

        public ExtBoxController(ExtBox ebox, Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested, bool editMode) 
            : base(blockGotFocusHandler, collapseRequested)
        {
            FinishConstructor(ebox, editMode);
        }

        void FinishConstructor(ExtBox ebox, bool editMode)
        {
            VM = new ExtBoxVM(ebox, VMGotFocus, HandleCommand);
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
            if (UIGlobals.Deferred.OnNewBox != null)
            {
                VM.ParentId = UIGlobals.Deferred.OnNewBox.ParentId;
                LinkToPersonIdOnSave = UIGlobals.Deferred.OnNewBox.LinkedPersonId;
                UIGlobals.Deferred.OnNewBox = null;
            }
            if (UIGlobals.Deferred.OnOpenBox != null)
            {
                if (UIGlobals.Deferred.OnOpenBox.MakeUndone)
                    VM.DoneDate = null;
                UIGlobals.Deferred.OnOpenBox = null;
            }
        }

        public void ReloadLinks()
        {
            Globals.UI.UpdateLinks(VM.Persistent);
            VM.InitializeLinksFromPersistent();
            VM.Links.Touch();
        }

        public override void AfterReinflated()
        {
            VM.IsEditMode = true;
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
                    Globals.UI.SaveBox(VM.Persistent, true);
                    if (LinkToPersonIdOnSave != null)
                    {
                        Globals.UI.WritePersonLink(new LinkInstruction
                        {
                            Link = LinkType.FromBoxToPerson,
                            FromId = VM.Persistent.Box.RowId,
                            ToId = LinkToPersonIdOnSave.Value
                        });
                        LinkToPersonIdOnSave = null;
                    }
                    VM.IsDirty = false;
                }
                catch (Exception ex)
                {
                    failedSave = true;
                    UIGlobals.Do.ShowTimedMessge("Cannot save: " + ex.Message);
                }
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
                Globals.UI.AbandonBox(VM.Persistent.Box.RowId);
                CollapseRequested(this, VM.Persistent.Box.RowId == 0);
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
                bool saveOK = ChangeMode(Mode.ReadOnly, true);
                if (!VM.IsUnclass && saveOK)
                    CollapseRequested(this, false);
                return true;
            }
            if (command == Globals.Commands.NEWLINKEDBOX)
            {
                //if not saved, save to get parent id
                if (VM.Persistent.Box.RowId == 0)
                {
                    if (!ChangeMode(Mode.Edit, true)) return true; 
                }
                UIGlobals.Deferred.OnNewBox = new DeferredBehaviors.NewBoxBehavior { ParentId = VM.Persistent.Box.RowId };
                UIGlobals.Do.HandleGlobalCommand(Globals.Commands.NEWITEM);
            }
            if (command == Globals.Commands.EDITLINKS)
            {
                UIGlobals.RecordLinkController.ActivateFor(this);
            }
            if (command == Globals.Commands.CLASSIFY)
            {
                HandleClassifyCommand();
                return true;
            }
            if (command == Globals.Commands.RESCHEDULE)
            {
                string newDate = RescheduleDialog.ShowDialog(VM.BoxTime_Date.Date);
                if (newDate != null)
                {
                    VM.BoxTime_Date.Date = newDate;
                    UIGlobals.Do.ShowTimedMessge("Rescheduled for " + DateUtil.ToReadableDate(newDate, includeDOW: true));
                    ChangeMode(Mode.ReadOnly, true);
                    CollapseRequested(this, false);
                }
                return true;
            }
            if (command == Globals.Commands.DONE)
            {
                HandleDoneCommand();
                return true;
            }
            if (command == Globals.Commands.IMPORTEXPORT)
            {
                var childIds = VM.Links.Items.Where(r => r.Link == LinkType.FromBoxToChildBox).Select(r => r.OtherId).ToArray();
                if (childIds.Length == 0) return false;
                ExportHtmlDialog.ShowExportDialog(childIds, null);
                return true;
            }
            if (command == Globals.Commands.SELECTFOLDER)
            {
                var initialPath = GetDefaultFolderPath();
                var dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                if (initialPath != null) dlg.SelectedPath = initialPath;
                if (dlg.ShowDialog(App.Current.MainWindow) != true) return true;
                VM.RefDir = dlg.SelectedPath;
                VM.NotifyVisibilityDetails();
                return true;
            }
            if (command == Globals.Commands.SELECTFILE)
            {
                var initialPath = GetDefaultFolderPath();
                var dlg = new OpenFileDialog();
                if (initialPath != null) dlg.InitialDirectory = initialPath;
                if (dlg.ShowDialog(App.Current.MainWindow) != true) return true;
                VM.RefFile = dlg.FileName;
                VM.NotifyVisibilityDetails();
                return true;
            }
            if (command == Globals.Commands.OPENFOLDER)
            {
                if (!string.IsNullOrEmpty(VM.RefDir))
                    VisualUtils.OpenWithWithDefaultApp(VM.RefDir);
                return true;
            }
            if (command == Globals.Commands.OPENFILE)
            {
                if (!string.IsNullOrEmpty(VM.RefFile))
                    VisualUtils.OpenWithWithDefaultApp(VM.RefFile);
                return true;
            }
            if (command == Globals.Commands.CREATEFILE)
            {
                HandleCreateFileCommand();
                return true;
            }
            if (command == Globals.Commands.CAPTUREEMAIL)
            {
                string s = Clipboard.GetText();
                if (string.IsNullOrEmpty(s))
                {
                    VisualUtils.ShowMessageDialog("No text found on clipboard. (From Thunderbird, use Ctrl-UAC to copy it.");
                    return true;
                }
                VM.RawEmail.Value = s;
                VM.NotifyVisibilityDetails();
                UIGlobals.Do.ShowTimedMessge("Email captured");
                return true;
            }
            if (command == Globals.Commands.VIEWEMAIL)
            {
                string s = Clipboard.GetText();
                if (!VM.RawEmail.HasValue)
                {
                    VisualUtils.ShowMessageDialog("No email was captured into this task.");
                    return true;
                }
                string filename = Path.Combine(Path.GetTempPath(), "systematizer.eml");
                File.WriteAllText(filename, VM.RawEmail.Value);
                VisualUtils.OpenWithWithDefaultApp(filename);
                return true;
            }
            if (command == Globals.Commands.NEWLINKEDPERSON)
            {
                if (VM.Persistent.Box.RowId == 0) return true;
                UIGlobals.Deferred.OnNewPerson = new DeferredBehaviors.NewPersonBehavior { LinkedBoxId = VM.Persistent.Box.RowId };
                UIGlobals.Do.HandleGlobalCommand(Globals.Commands.NEWPERSON);
                return true;
            }
            return false;
        }

        void HandleCreateFileCommand()
        {
            //if no template folder or if it's empty, explain to user how to use the command and abort
            bool found = false;
            string[] templateNames = null;
            string templatePath = Path.Combine(Globals.UI.GetExeDirectory(), "templates");
            if (Directory.Exists(templatePath))
            {
                var fullNames = Directory.GetFiles(templatePath);
                if (fullNames.Any())
                {
                    found = true;
                    templateNames = fullNames.Select(s => Path.GetFileName(s)).ToArray();
                }
            }
            if (!found)
            {
                VisualUtils.ShowMessageDialog("To use this feature, create a folder called 'Templates' in the application folder, and manually copy your templates into that folder.");
                return;
            }

            //get user choice, or if just 1, use that
            string templateName = templateNames[0]; //without path
            if (templateNames.Length > 1)
            {
                int idx = SelectDialog.SelectFromList(templateNames.ToList());
                if (idx < 0) return;
                templateName = templateNames[idx];
            }

            //find default starting path for target file
            string defaultPath = GetDefaultFolderPath();

            //choose destination folder and name
            var saveDlg = new SaveFileDialog();
            if (defaultPath != null) saveDlg.InitialDirectory = defaultPath;
            string ext = Path.GetExtension(templateName);
            saveDlg.Filter = ext + "|" + ext;
            if (saveDlg.ShowDialog(App.Current.MainWindow) != true) return;
            string targetName = saveDlg.FileName; //with path

            //copy file then open it
            File.Copy(Path.Combine(templatePath, templateName), targetName);
            VisualUtils.OpenWithWithDefaultApp(targetName);

            VM.NotifyVisibilityDetails();
        }

        void HandleDoneCommand()
        {
            string userMessage = null;
            bool doCollapseBlock, doRemoveBlock = false, doMarkDone = false;

            //note we will save VM to persistent so we project using the correct repeats, but then we will modify the VM again
            VM.WriteToPersistent();

            //handle repeated task projection
            if (VM.Persistent.Repeats != null)
            {
                var projector = new RepeatProjector();
                string nextTime = projector.AdvanceTime(VM.Persistent.Box.BoxTime, VM.Persistent.Repeats);
                if (nextTime != null)
                {
                    //reschedule but don't mark done
                    VM.BoxTime_Date.Date = nextTime;
                    VM.BoxTime_Time.Time = nextTime;
                    userMessage = "Recheduled for " + DateUtil.ToReadableDate(nextTime, includeDOW: true);
                    doCollapseBlock = true;
                }
                else
                {
                    //reached the last instance of a repeated task
                    doMarkDone = true;
                    doCollapseBlock = true;
                }
            }
            else doMarkDone = doCollapseBlock = doRemoveBlock = true;

            //check if it is possible to really mark it done
            if (doMarkDone)
            {
                userMessage = "Task done!";
                bool hasUndoneChildren = Globals.UI.CheckBoxesExist(parentRowId: VM.Persistent.Box.RowId, filterByNotDone: true);
                if (hasUndoneChildren)
                {
                    doMarkDone = false;
                    doCollapseBlock = doRemoveBlock = false;
                    userMessage = "Cannot complete task because it has child items that are not done";
                }
                else if (VM.Importance == Constants.IMPORTANCE_HIGH)
                {
                    if (!VisualUtils.Confirm("Task is marked important. About to remove from calendar."))
                    {
                        doMarkDone = false;
                        doCollapseBlock = doRemoveBlock = false;
                        userMessage = null;
                    }
                }
            }

            if (doMarkDone)
                VM.DoneDate = DateUtil.ToYMD(DateTime.Today);
            if (doCollapseBlock)
            {
                ChangeMode(Mode.ReadOnly, true);
                CollapseRequested(this, doRemoveBlock);
            }
            if (userMessage != null)
                UIGlobals.Do.ShowTimedMessge(userMessage);
        }

        void HandleClassifyCommand()
        {
            //get the box to classify; abort if already classified
            var box = VM.Persistent.Box;
            if (box.IsUnclass == 0) return;

            //build presets list (BoxCreator values are offset by 1 in the options list)
            var options = new List<string>();
            options.Add("Note");
            if (Globals.AllowTasks) options.AddRange(BoxCreator.NAMES);

            //ask which preset to use
            int selectedPreset = SelectDialog.SelectFromList(options);
            if (selectedPreset < 0) return;

            //note we save the VM, edit the persistent object, then reload the VM; but nothing is actually saved to db
            VM.WriteToPersistent();
            box.IsUnclass = 0;

            //if classifying as note, just clear IsUnclass
            //else copy from BoxCreator's template
            if (selectedPreset > 0)
            {
                var templateBox = BoxCreator.GetPreset(selectedPreset - 1);
                box.BoxTime = DateUtil.ToYMD(DateTime.Today) + "0900";
                box.Visibility = templateBox.Visibility;
                box.TimeType = templateBox.TimeType;
                box.Importance = templateBox.Importance;
                box.Duration = templateBox.Duration;
                box.PrepDuration = templateBox.PrepDuration;
            }
            VM.InitializeFromPersistent();
            VM.IsDirty = true;
        }

        /// <summary>
        /// Find the most likely folder for new/select file, based on this RefDir, ancestor RefDir, then this RefFile, then ancestor RefFile
        /// </summary>
        /// <returns>null if none</returns>
        string GetDefaultFolderPath()
        {
            if (!string.IsNullOrEmpty(VM.RefDir)) return VM.RefDir;
            var boxWithPath = Globals.UI.NavigateToParentBoxWhere(VM.Persistent.Box, b => !string.IsNullOrEmpty(b.RefDir));
            if (boxWithPath != null)
                return boxWithPath.RefDir;
            if (!string.IsNullOrEmpty(VM.RefFile)) return Path.GetDirectoryName(VM.RefDir);
            boxWithPath = Globals.UI.NavigateToParentBoxWhere(VM.Persistent.Box, b => !string.IsNullOrEmpty(b.RefFile));
            if (boxWithPath != null)
                return Path.GetDirectoryName(boxWithPath.RefFile);
            return null;
        }
    }
}
