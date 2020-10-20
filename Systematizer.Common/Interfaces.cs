using System;

namespace Systematizer.Common
{
    /// <summary>
    /// Defines action that the UI must be able to do when called from common layer
    /// </summary>
    public interface IUIAction
    {
        /// <summary>
        /// Show OS level notification even when app is not showing
        /// </summary>
        void ShowToasterNotification(string message);

        /// <summary>
        /// Set the UI in or out of idle mode; also see UIService.RequestWakeUp
        /// </summary>
        void SetIdleMode(bool idle);

        /// <summary>
        /// Called when any box changed
        /// </summary>
        void BoxCacheChanged(BoxEditingPool.Item changes);
    }
}
