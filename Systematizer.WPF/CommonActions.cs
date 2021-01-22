﻿using System;
using System.Media;
using Systematizer.Common;
using Notifications.Wpf.Core;

namespace Systematizer.WPF
{
    /// <summary>
    /// Implementation of actions called by Common layer
    /// </summary>
    class CommonActions : IUIAction
    {
        readonly NotificationManager Toaster = new NotificationManager();

        public void BoxCacheChanged(BoxEditingPool.Item changes)
        {
            UIGlobals.Do.RebuildViews(changes);
        }

        public void SetIdleMode(bool idle)
        {
            if (idle)
            {
                //save chunk arrangements for today and tomorrow
                UIGlobals.Do.SaveAll(false);
            } 
            else //wakeup
            {
                UIGlobals.Do.RebuildViews(null);
            }

            UIGlobals.Do.ShowHideIdleMode(idle);
        }

        public void ShowToasterNotification(string message, bool extraTime)
        {
            //the toast component doesn't show if app is minimized, so fix that
            UIGlobals.Do.EnsureNotMinimized();

            int seconds = extraTime ? 120 : 12;
            Toaster.ShowAsync(new NotificationContent
            {
                Title = "Systematizer",
                Message = message,
                Type = NotificationType.Success
            }, expirationTime: TimeSpan.FromSeconds(seconds));
            SystemSounds.Exclamation.Play();
        }
    }
}
