using System;

namespace Systematizer.Common
{
    /// <summary>
    /// Enumeration of available menu commands, desktop key bindings, and injected handlers
    /// </summary>
    public class CommandCenter
    {
        const string 
            SECTION_TOP = "Main",
            SECTION_EDIT = "Edit",
            SECTION_CREATE = "Create",
            SECTION_BOXDETAIL = "Special task management",
            SECTION_PERSONDETAIL = "Special person management",
            SECTION_UTIL = "Utilities";

        public class Item
        {
            public string KeyShortcut; //F1-12 or CF1-CF12 or CA-CZ or C1-C9 for ctrl and function keys (all cases other than letters and func keys require explicit code in UI)
            public string KeyDescriptionOverride; //optional, overrides menu display
            public string MenuSection; //null means don't show on menu
            public string Description;

            public string KeyDescription()
            {
                if (KeyDescriptionOverride != null) return KeyDescriptionOverride;
                if (KeyShortcut != null)
                {
                    if (KeyShortcut[0] == 'C') return "Ctrl-" + KeyShortcut.Substring(1);
                    return KeyShortcut;
                }
                return "";
            }
        }

        public Item[] ALL;

        //SECTION_TOP
        public readonly Item OPENMENU = new Item
        {
            Description = "Open menu",
            MenuSection = SECTION_TOP,
            KeyDescriptionOverride = "Ctrl-Space"
        };
        public readonly Item FINDPERSON = new Item
        {
            Description = "Find person",
            MenuSection = SECTION_TOP,
            KeyShortcut = "CF"
        };
        public readonly Item FINDBOX = new Item
        {
            Description = "Find task/note",
            MenuSection = SECTION_TOP,
            KeyShortcut = "F2"
        };
        public readonly Item OPEN = new Item
        {
            Description = "Open",
            MenuSection = SECTION_TOP,
            KeyDescriptionOverride = "Enter"
        };
        public readonly Item NAVNEXTBLOCK = new Item
        {
            Description = "Navigate blocks",
            MenuSection = SECTION_TOP,
            KeyDescriptionOverride = "Ctrl-Arrows"
        };
        public readonly Item NAVPRIORBLOCK = new Item
        {
            Description = "Navigate blocks"
        };
        public readonly Item NAVOTHERSTACK = new Item
        {
            Description = "Navigate blocks"
        };

        //SECTION_CREATE
        public readonly Item NEWUNCLASSIFIED = new Item
        {
            Description = "New quick note",
            MenuSection = SECTION_CREATE,
            KeyShortcut = "F12"
        };
        public readonly Item NEWITEM = new Item
        {
            Description = "New...",
            MenuSection = SECTION_CREATE,
            KeyShortcut = "F1"
        };
        public readonly Item NEWLINKEDBOX = new Item
        {
            Description = "New sub-item",
            MenuSection = SECTION_CREATE,
            KeyShortcut = "CB"
        };
        public readonly Item NEWPERSON = new Item
        {
            Description = "New person",
            MenuSection = SECTION_CREATE,
            KeyShortcut = "CP"
        };

        //SECTION_EDIT
        public readonly Item ENDEDITS = new Item
        {
            Description = "End edits",
            KeyDescriptionOverride = "Ctrl-Enter",
            MenuSection = SECTION_EDIT
        };
        public readonly Item CLOSE = new Item
        {
            Description = "Save, collapse",
            MenuSection = SECTION_EDIT,
            KeyDescriptionOverride = "Esc"
        };
        public readonly Item EDITLINKS = new Item
        {
            Description = "Manage links",
            MenuSection = SECTION_EDIT,
            KeyShortcut = "C6"
        };
        public readonly Item ABANDON = new Item
        {
            Description = "Abandon edits",
            MenuSection = SECTION_EDIT
        };

        //SECTION_BOXDETAIL
        public readonly Item CLASSIFY = new Item
        {
            Description = "Classify quick note",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "F7"
        };
        public readonly Item RESCHEDULE = new Item
        {
            Description = "Reschedule",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "CR"
        };
        public readonly Item DONE = new Item
        {
            Description = "Done",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "CD"
        };
        public readonly Item NEWLINKEDPERSON = new Item
        {
            Description = "New linked person",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "CK"
        };
        public readonly Item SELECTFOLDER = new Item
        {
            Description = "Select folder",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "CF3"
        };
        public readonly Item OPENFOLDER = new Item
        {
            Description = "Open folder",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "F3"
        };
        public readonly Item SELECTFILE = new Item
        {
            Description = "Select file",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "CF4"
        };
        public readonly Item OPENFILE = new Item
        {
            Description = "Open file",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "F4"
        };
        public readonly Item CREATEFILE = new Item
        {
            Description = "Create file from template",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "F5"
        };
        public readonly Item VIEWEMAIL = new Item
        {
            Description = "View captured email",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "F6"
        };
        public readonly Item CAPTUREEMAIL = new Item
        {
            Description = "Capture email from clipboard",
            MenuSection = SECTION_BOXDETAIL,
            KeyShortcut = "CF6"
        };

        //SECTION_PERSONDETAIL
        public readonly Item EDITCATEGORIES = new Item
        {
            Description = "Edit categories for person",
            MenuSection = SECTION_PERSONDETAIL,
            KeyShortcut = "F8"
        };
        public readonly Item SENDEMAIL = new Item
        {
            Description = "Send email",
            MenuSection = SECTION_PERSONDETAIL,
            KeyShortcut = "F9"
        };
        public readonly Item DELETEPERSON = new Item
        {
            Description = "Delete person",
            MenuSection = SECTION_PERSONDETAIL
        };

        //SECTION_UTIL
        public readonly Item SETTINGS = new Item
        {
            Description = "Settings",
            MenuSection = SECTION_UTIL,
            KeyShortcut = "C0"
        };
        public readonly Item IMPORTEXPORT = new Item
        {
            Description = "Import or export",
            MenuSection = SECTION_UTIL
        };
        public readonly Item MANAGECATS = new Item
        {
            Description = "Manage categories for people",
            MenuSection = SECTION_UTIL,
            KeyShortcut = "CF8"
        };

        public CommandCenter()
        {
            ALL = new[] { 
                //SECTION_TOP
                OPENMENU, FINDPERSON, FINDBOX, OPEN, NAVPRIORBLOCK, NAVNEXTBLOCK, NAVOTHERSTACK,

                //SECTION_CREATE
                NEWUNCLASSIFIED, NEWITEM, NEWLINKEDBOX, NEWPERSON, 

                //SECTION_EDIT
                ENDEDITS, CLOSE, EDITLINKS, ABANDON, 

                //SECTION_BOXDETAIL
                CLASSIFY, RESCHEDULE, DONE, NEWLINKEDPERSON, SELECTFOLDER, OPENFOLDER, SELECTFILE, OPENFILE, CREATEFILE,
                CAPTUREEMAIL, VIEWEMAIL,

                //SECTION_PERSONDETAIL
                EDITCATEGORIES, SENDEMAIL, DELETEPERSON,

                //SECTION_UTIL
                SETTINGS, MANAGECATS, IMPORTEXPORT
            };
        }
    }
}
