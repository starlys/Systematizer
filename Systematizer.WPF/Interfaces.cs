using System;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Behaviors that require context and need to be called from everywhere.
    /// Comments are in the implementing class: MainController
    /// </summary>
    interface IGlobalBehaviors
    {
        void FocusTopBlock();
        void UserActionCompleted(bool potentialChange);
        void RebuildViews(BoxEditingPool.Item changes, bool isNewDay);
        bool SaveAll(bool includeUserExplicit);
        void ShowHideIdleMode(bool idle);
        void ShowTimedMessge(string msg);
        void AddToTop(bool useEditStack, Func<Action<BlockController>, Action<BlockController, bool>, BlockController> creatorFunc);
        void AddBoxToEditStack(ExtBox ebox);
        void AddPersonToEditStack(ExtPerson ep);
        void ToggleMenu(bool? mode);
        bool OpenDatabaseWithErrorReporting(string path);
        bool HandleGlobalCommand(CommandCenter.Item item);
        void NavigateBlock(bool goToOtherStack, int delta);
        void OpenBlockFromLink(LinkType link, long rowId);
    }

    /// <summary>
    /// Applied to classes that store data that stays with a collapsed block and can be used to reinflate it
    /// </summary>
    interface ICollapseBlockData
    {
    }
}
