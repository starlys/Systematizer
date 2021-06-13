using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Controller for MainWindow. This contains controllers for stacks and handles main window, menu, idle dialog internally.
    /// </summary>
    class MainController : IGlobalBehaviors //note this is the only not not derived from BaseController
    {
        MainWindow Win;
        bool IsBigMode = true;
        readonly WholeVM WholeVM = new();
        DispatcherTimer Timer30; //30s timer as required by common layer
        readonly UICommandCenter Commands = new();
        BlockStackController FocusedStack;
        bool IsIdlePanelShowing;

        BlockStackController HomeStackController;
        BlockStackController EditStackController;

        //called from MainWindow codebehind
        public void Initialize(MainWindow w)
        {
            //setup window
            Win = w;
            HookupEventHandlers();
            ShowTimedMessge("Welcome");
            ResizeContents(Win.ActualWidth);

            //hook up UI layer to common layer
            Globals.UIAction = UIGlobals.CommonActions;

            //connect controller/view/viewmodel tree (with no vm data yet)
            HomeStackController = new BlockStackController(WholeVM.HomeStack, 
                () =>
                {
                    EditStackController.LoseFocus();
                    FocusedStack = HomeStackController;
                });
            Win.eHomeStack.DataContext = WholeVM.HomeStack;
            EditStackController = new BlockStackController(WholeVM.EditStack, 
                () =>
                {
                    HomeStackController.LoseFocus();
                    FocusedStack = EditStackController;
                });
            Win.eEditStack.DataContext = WholeVM.EditStack;
            Win.eRecordLinks.DataContext = UIGlobals.RecordLinkController.VM;

            //open default db or show settings dialog to select file
            redoAutoOpen:
            string path = RecentFilesList.GetFileToAutoOpen();
            if (path == null)
                HandleGlobalCommand(Globals.Commands.SETTINGS);
            else
                OpenDatabaseWithErrorReporting(path);

            //force open/create something
            if (Globals.DatabasePath == null) goto redoAutoOpen;

            //set up 30s timer
            Timer30 = new DispatcherTimer();
            Timer30.Tick += (s, e) =>
            {
                double idleSeconds = DateTime.UtcNow.Subtract(UIGlobals.LastActivityUtc).TotalSeconds;
                Globals.UI.Ping30((int)idleSeconds);
            };
            Timer30.Interval = TimeSpan.FromSeconds(30); 
            Timer30.Start();
        }

        /// <summary>
        /// Close database, update views and manage recent file list
        /// </summary>
        /// <returns>true on success; false means it did not close because of a validation error</returns>
        public bool CloseCurrentDatabase()
        {
            if (Globals.DatabasePath == null) return true;
            if (!SaveAll(true)) return false;
            RecentFilesList.RecordIsClosed(Globals.DatabasePath);
            Globals.UI.CloseDatabase();
            HomeStackController.Clear();
            EditStackController.Clear();
            return true;
        }

        /// <summary>
        /// Focus top open block in home stack; should call this after collapsing anything
        /// </summary>
        public void FocusTopBlock()
        {
            HomeStackController.FocusFirstUncollapsed();
        }

        /// <summary>
        /// Add controller/VM to top of a stack
        /// </summary>
        /// <param name="creatorFunc">function accepting handler for block getting focus, and handler for collapsing block, in that order</param>
        public void AddToTop(bool useEditStack, Func<Action<BlockController>, Action<BlockController, bool>, BlockController> creatorFunc)
        {
            if (useEditStack)
                EditStackController.AddToTop(creatorFunc);
            else
                HomeStackController.AddToTop(creatorFunc);
        }

        public void OpenBlockFromLink(LinkType link, long rowId)
        {
            if (link == LinkType.FromBoxToChildBox || link == LinkType.FromBoxToParentBox || link == LinkType.FromPersonToBox)
            {
                var ebox = Globals.UI.LoadBoxForEditing(rowId);
                if (ebox != null) AddBoxToEditStack(ebox);
            }
            if (link == LinkType.FromBoxToPerson || link == LinkType.FromPersonToPerson)
            {
                var ep = Globals.UI.LoadPerson(rowId);
                if (ep != null) AddPersonToEditStack(ep);
            }
        }

        public void AddBoxToEditStack(ExtBox ebox)
        {
            EditStackController.Add(ebox, true, true);
        }

        public void AddPersonToEditStack(ExtPerson ep)
        {
            EditStackController.Add(ep, true, false);
        }

        /// <summary>
        /// sets all to readonly, saving changes
        /// </summary>
        /// <returns>true on success</returns>
        bool EndEditAndSave()
        {
            bool success1 = HomeStackController.ChangeMode(BaseController.Mode.ReadOnly, true);
            bool success2 = EditStackController.ChangeMode(BaseController.Mode.ReadOnly, true);
            return success1 && success2;
        }

        /// <summary>
        /// Save all or some blocks
        /// </summary>
        /// <param name="includeUserExplicit">if true, saves all blocks; if false, only saves incidental view info like day chunks but does not save
        /// boxes or people blocks</param>
        /// <returns>true if success</returns>
        public bool SaveAll(bool includeUserExplicit)
        {
            if (includeUserExplicit)
                return EndEditAndSave();
            foreach (var c in HomeStackController.GetByType<TodayController>())
                c.ChangeMode(BaseController.Mode.ReadOnly, true);
            return true;
        }

        /// <summary>
        /// Open database, load views and manage recent file list
        /// (Closes existing if any)
        /// </summary>
        /// <returns>true on success, else shows error</returns>
        public bool OpenDatabaseWithErrorReporting(string path)
        {
            CloseCurrentDatabase();
            if (!Globals.UI.OpenDatabase(path))
            {
                VisualUtils.ShowMessageDialog("File not found or could not be opened");
                return false;
            }

            RecentFilesList.RecordIsOpen(path); 
            
            //init controllers and load default views
            HomeStackController.InitializeHomeStack();
            EditStackController.InitializeEditStack();
            return true;
        }

        /// <summary>
        /// Rebuilds list views in home stack in response to a single edit.
        /// Note that this should called after many operations in case the views need to show tasks that came into view since the last refresh.
        /// </summary>
        /// <param name="changes">can be null if this is called just because of elapsed time; otherwise it holds the edit that was made</param>
        public void RebuildViews(BoxEditingPool.Item changes, bool isNewDay)
        {
            if (isNewDay)
                HomeStackController.InitializeHomeStack();
            HomeStackController.RefreshAllLists(changes);
        }

        void HookupEventHandlers()
        {
            Win.SizeChanged += (s, e) =>
            {
                ResizeContents(e.NewSize.Width);
                UIGlobals.WindowAffectsPopupAction?.Invoke();
            };
            Win.LocationChanged += (s, e) =>
            {
                UIGlobals.WindowAffectsPopupAction?.Invoke();
            };
            Win.StateChanged += (s, e) =>
            {
                ShowHideIdleMode(false);
                UIGlobals.WindowAffectsPopupAction?.Invoke();
            };
            Win.eMenuButton.Click += (s, e) =>
            {
                ToggleMenu(null);
            };

            Win.eQuickNoteButton.Click += (s, e) =>
            {
                HandleGlobalCommand(Globals.Commands.NEWUNCLASSIFIED);
            };
            Win.PreviewKeyDown += (s, e) =>
            {
                UIGlobals.LastActivityUtc = DateTime.UtcNow;
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                bool isShift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                bool isCtrlOnly = isCtrl && !isShift;
                var item = UICommandCenter.KeyToItem_Early(e.Key, isCtrlOnly);
                if (item != null)
                {
                    if (HandleGlobalCommand(item))
                        ToggleMenu(false);
                    e.Handled = true;
                }
            };
            Win.KeyDown += (s, e) =>
            {
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                var item = UICommandCenter.KeyToItem_Late(e.Key, isCtrl);
                if (item != null)
                {
                    if (HandleGlobalCommand(item))
                        ToggleMenu(false);
                    e.Handled = true;
                }
            };
            Win.Closing += (s, e) =>
            {
                if (!CloseCurrentDatabase())
                {
                    e.Cancel = true;
                    return;
                }
                ShowTimedMessge("Ciao!");
                Timer30.IsEnabled = false;
                UIGlobals.CommonActions.Cleanup();
                App.Current.Shutdown();
            };
            Win.eWakeUp.Click += (s, e) => ShowHideIdleMode(false);
        }

        /// <summary>
        /// show timed message in screen top bar; also see CommonActions for notifications off screen
        /// </summary>
        /// <param name="msg"></param>
        public async void ShowTimedMessge(string msg)
        {
            Win.eMessage.Text = msg;
            Win.eMessage.Visibility = Visibility.Visible;
            await Task.Delay(6000);
            Win.eMessage.Visibility = Visibility.Collapsed;
        }

        /// <param name="mode">true to open, false to close, null to switch</param>
        public void ToggleMenu(bool? mode)
        {
            bool isMenuOpen = Win.eMenu.Visibility == Visibility.Visible;
            if (mode == isMenuOpen) return;
            bool shouldOpen = mode ?? !isMenuOpen;
            if (shouldOpen)
            {
                if (Win.eMenu.eMenuRoot.Children.Count == 0) InitializeMenu();
                Win.eMenu.Visibility = Visibility.Visible;
            }
            else
            {
                Win.eMenu.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowHideIdleMode(bool idle)
        {
            if (IsIdlePanelShowing == idle) return;
            if (idle)
            {
                Win.eNonIdlePanel.Visibility = Visibility.Collapsed;
                Win.eIdlePanel.Visibility = Visibility.Visible;
                IsIdlePanelShowing = true;
                VisualUtils.DelayThen(100, () => Win.eWakeUp.Focus());
            }
            else
            {
                Win.eIdlePanel.Visibility = Visibility.Collapsed;
                Win.eNonIdlePanel.Visibility = Visibility.Visible;
                IsIdlePanelShowing = false;
                UserActionCompleted(true);
                Globals.UI.RequestWakeUp();
            }
        }

        /// <summary>
        /// Handle any command defined in common layer. First gets the focused block to handle it, then try global implementation.
        /// </summary>
        /// <returns>true if canceled</returns>
        public bool HandleGlobalCommand(CommandCenter.Item item)
        {
            bool? handled = FocusedStack?.HandleCommand(item);
            if (handled == true) return true;
            return Commands.HandleGlobalItem(item);
        }

        void InitializeMenu()
        {
            void clickHandler(object s, MouseButtonEventArgs e)
            {
                var item = ((StackPanel)s).DataContext as CommandCenter.Item;
                ToggleMenu(false);
                HandleGlobalCommand(item);
            };

            var sectionParent = Win.eMenu.eMenuRoot;
            var headerStyle = Win.eMenu.Resources["Header"] as Style;
            var shortcutStyle = Win.eMenu.Resources["Shortcut"] as Style;
            var itemStyle = Win.eMenu.Resources["Item"] as Style;
            foreach (var section in Globals.Commands.ALL.GroupBy(c => c.MenuSection))
            {
                if (string.IsNullOrEmpty(section.Key)) continue;
                var sectionStack = new StackPanel();
                sectionParent.Children.Add(sectionStack);
                sectionStack.Children.Add(new TextBlock
                {
                    Text = section.Key,
                    Style = headerStyle
                });
                foreach (var cmd in section)
                {
                    var hstack = new StackPanel { Orientation = Orientation.Horizontal, DataContext = cmd, Cursor = Cursors.Hand };
                    hstack.MouseDown += clickHandler;
                    sectionStack.Children.Add(hstack);
                    hstack.Children.Add(new TextBlock
                    {
                        Text = cmd.KeyDescription(),
                        Style = shortcutStyle
                    });
                    hstack.Children.Add(new TextBlock
                    {
                        Text = cmd.Description,
                        Style = itemStyle
                    });
                }
            }
        }

        void ResizeContents(double width)
        {
            bool big = width > 700;
            if (big)
            {
                if (!IsBigMode)
                {
                    IsBigMode = true;
                    Win.eTopEditContainer.Children.Remove(Win.eEditStack);
                    Win.eSideEditContainer.Children.Add(Win.eEditStack);
                }
                double halfw = Math.Min(800, (width - 30) / 2 - 10);
                Win.eSideEditContainer.Width = halfw;
                Win.eHomeStack.Width = halfw;
            }
            else
            {
                if (IsBigMode)
                {
                    IsBigMode = false;
                    Win.eSideEditContainer.Children.Remove(Win.eEditStack);
                    Win.eTopEditContainer.Children.Add(Win.eEditStack);
                }
                Win.eSideEditContainer.Width = 0;
                Win.eHomeStack.Width = width - 44;
            }
        }

        /// <summary>
        /// Global handler for completing any user action like open/close
        /// </summary>
        public void UserActionCompleted(bool potentialChange)
        {
            UIGlobals.LastActivityUtc = DateTime.UtcNow;
            HomeStackController.RemoveOldCollapsed();
            EditStackController.RemoveOldCollapsed();
        }

        public void NavigateBlock(bool goToOtherStack, int delta)
        {
            var stack = goToOtherStack ? (FocusedStack == HomeStackController ? EditStackController : HomeStackController) : FocusedStack;
            stack.FocusDelta(delta);
        }
    }
}
