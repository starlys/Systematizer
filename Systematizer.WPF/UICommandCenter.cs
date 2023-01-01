using System.Windows.Input;
using Systematizer.Common.PersistentModel;

namespace Systematizer.WPF;

/// <summary>
/// UI level implementation of commands; also see CommandCenter
/// </summary>
class UICommandCenter
{
    readonly Dictionary<CommandCenter.Item, Func<bool>> Handlers = new();

    public UICommandCenter()
    {
        //SECTION_TOP
        Handlers[Globals.Commands.OPENMENU] = () =>
        {
            UIGlobals.Do.ToggleMenu(null);
            return false; //not handled so that caller doesn't close menu after this
        };
        Handlers[Globals.Commands.FINDPERSON] = () =>
        {
            UIGlobals.Do.AddToTop(false, (a, b) => new PersonSearchController(a, b));
            return true;
        };
        Handlers[Globals.Commands.FINDBOX] = () =>
        {
            UIGlobals.Do.AddToTop(false, (a, b) => new BoxSearchController(a, b, false));
            return true;
        };
        Handlers[Globals.Commands.NAVPRIORBLOCK] = () =>
        {
            UIGlobals.Do.NavigateBlock(false, -1);
            return true;
        };
        Handlers[Globals.Commands.NAVNEXTBLOCK] = () =>
        {
            UIGlobals.Do.NavigateBlock(false, +1);
            return true;
        };
        Handlers[Globals.Commands.NAVOTHERSTACK] = () =>
        {
            UIGlobals.Do.NavigateBlock(true, 0);
            return true;
        };

        //SECTION_CREATE
        Handlers[Globals.Commands.NEWUNCLASSIFIED] = () =>
        {
            var box = BoxCreator.GetUnclassified();
            var ebox = new ExtBox(box, null);
            UIGlobals.Do.AddBoxToEditStack(ebox);
            return true;
        };
        Handlers[Globals.Commands.NEWITEM] = () =>
        {
            //build presets list (BoxCreator values are offset by 2 in the options list)
            var options = new List<string>
            {
                "Quick note",
                "Note"
            };
            if (Globals.AllowTasks) options.AddRange(BoxCreator.NAMES);

            //ask which preset to use
            int selectedPreset = SelectDialog.SelectFromList(options);
            if (selectedPreset < 0)
            {
                UIGlobals.Deferred.OnNewBox = null;
                return true;
            }

            //create box
            Box box;
            if (selectedPreset == 0)
                box = BoxCreator.GetUnclassified();
            else if (selectedPreset == 1)
            {
                box = BoxCreator.GetUnclassified();
                box.IsUnclass = 0;
            }
            else
            {
                box = BoxCreator.GetPreset(selectedPreset - 2);
                box.BoxTime = DateUtil.ToYMD(DateTime.Today) + "0900";
            }

            var ebox = new ExtBox(box, null);
            UIGlobals.Do.AddBoxToEditStack(ebox);
            return true;
        };
        Handlers[Globals.Commands.NEWPERSON] = () =>
        {
            var ep = new ExtPerson(new Person(), null, null);
            UIGlobals.Do.AddToTop(true, (a, b) => new ExtPersonController(ep, a, b, false));
            return true;
        };

        //SECTION_EDIT

        //SECTION_UTIL
        Handlers[Globals.Commands.ABOUT] = () =>
        {
            AboutWindow.ShowAbout();
            return true;
        };
        Handlers[Globals.Commands.SETTINGS] = () =>
        {
            UIGlobals.Do.SaveAll(true);
            new SystemController().ShowDialog();
            return true;
        };
        Handlers[Globals.Commands.MANAGECATS] = () =>
        {
            UIGlobals.Do.SaveAll(true);
            CatManageDialog.ManageCats();
            return true;
        };
        Handlers[Globals.Commands.IMPORTEXPORT] = () =>
        {
            ExportHtmlDialog.ShowExportDialog(null, null);
            return true;
        };
    }

    /// <summary>
    /// Find command that goes with the given key, or null - for keys to check before focus element sees it
    /// </summary>
    /// <param name="isCtrlOnly">true if ctrl but no other modifier is down</param>
    public static CommandCenter.Item KeyToItem_Early(Key key, bool isCtrlOnly)
    {
        //manual mapping of the commands that aren't ctrl+letter
        if (key == Key.Space && isCtrlOnly) return Globals.Commands.OPENMENU;
        if (key == Key.Left && isCtrlOnly) return Globals.Commands.NAVOTHERSTACK;
        if (key == Key.Right && isCtrlOnly) return Globals.Commands.NAVOTHERSTACK;
        if (key == Key.Up && isCtrlOnly) return Globals.Commands.NAVPRIORBLOCK;
        if (key == Key.Down && isCtrlOnly) return Globals.Commands.NAVNEXTBLOCK;
        return null;

    }
    /// <summary>
    /// Find command that goes with the given key, or null - for keys to check after not handled by focus element
    /// </summary>
    public static CommandCenter.Item KeyToItem_Late(Key key, bool isCtrl)
    {
        //manual mapping of the commands that aren't ctrl+letter/number/function key
        if (key == Key.Enter && isCtrl) return Globals.Commands.ENDEDITS;
        if (key == Key.Enter) return Globals.Commands.OPEN;
        if (key == Key.Escape) return Globals.Commands.CLOSE;

        //for control and function keys, search for matching command
        string keyShortcut = null;
        if (isCtrl && key >= Key.A && key <= Key.Z) keyShortcut = KeyToKeyShortcut(key, true, false, Key.A, 'A');
        else if (isCtrl && key >= Key.D0 && key <= Key.D9) keyShortcut = KeyToKeyShortcut(key, true, false, Key.D0, '0');
        else if (key >= Key.F1 && key <= Key.F12) keyShortcut = KeyToKeyShortcut(key, isCtrl, true, Key.F1, '1');
        if (keyShortcut != null)
            return Globals.Commands.ALL.FirstOrDefault(c => c.KeyShortcut == keyShortcut);
        return null;
    }

    /// <summary>
    /// Handle a command that is defined here at global scope; also note that controllers for specific blocks can handle commands
    /// first when those commands need context.
    /// </summary>
    /// <returns>true if handled</returns>
    public bool HandleGlobalItem(CommandCenter.Item item)
    {
        UIGlobals.LastActivityUtc = DateTime.UtcNow;
        if (Handlers.TryGetValue(item, out var handler))
            return handler();
        return false;
    }

    /// <summary>
    /// Map from Key constants to strings as defined by CommandCenter.Item.KeyShortcut, or null
    /// </summary>
    static string KeyToKeyShortcut(Key key, bool isCtrl, bool isFunc, Key startOfRange1, char startOfRange2)
    {
        if (isFunc)
        {
            int fNo = ((int)key - (int)startOfRange1) + 1;
            if (isCtrl) return "CF" + fNo;
            return "F" + fNo;
        }
        int keyi = ((int)key - (int)startOfRange1) + (int)startOfRange2;
        char keyc = (char)keyi;
        if (isCtrl) return "C" + keyc;
        return null;
    }
}
