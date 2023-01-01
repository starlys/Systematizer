namespace Systematizer.Common;

/// <summary>
/// Defines action that the UI must be able to do when called from common layer
/// </summary>
public interface IUIAction
{
    /// <summary>
    /// Show OS level notification even when app is not showing
    /// </summary>
    /// <param name="extraTime">if true, shows for a long time for high importance</param>
    void ShowToasterNotification(string message, bool extraTime);

    /// <summary>
    /// Set the UI in or out of idle mode; also see UIService.RequestWakeUp
    /// </summary>
    void SetIdleMode(bool idle, bool isNewDay);

    /// <summary>
    /// Called when any box changed
    /// </summary>
    void BoxCacheChanged(BoxEditingPool.Item changes);
}
