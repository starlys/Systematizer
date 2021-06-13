using System;
using System.Media;
using Systematizer.Common;
using System.Windows;

using Toast = ToastNotifications;
using ToastL = ToastNotifications.Lifetime;
using ToastP = ToastNotifications.Position;
using ToastM = ToastNotifications.Messages;
using ToastC = ToastNotifications.Core;

namespace Systematizer.WPF
{
    /// <summary>
    /// Implementation of actions called by Common layer
    /// </summary>
    class CommonActions : IUIAction
    {
        Toast.Notifier ShortToaster;
        Toast.Notifier LongToaster;

        /// <summary>
        /// Clean up resources to prepare for exit
        /// </summary>
        public void Cleanup()
        {
            if (LongToaster != null)
            {
                LongToaster.Dispose();
                LongToaster = null;
            }
            if (ShortToaster != null)
            {
                ShortToaster.Dispose();
                ShortToaster = null;
            }
        }

        public void BoxCacheChanged(BoxEditingPool.Item changes)
        {
            UIGlobals.Do.RebuildViews(changes, false);
        }

        public void SetIdleMode(bool idle, bool isNewDay)
        {
            if (idle)
            {
                //save chunk arrangements for today and tomorrow
                UIGlobals.Do.SaveAll(false);
            } 
            else //wakeup
            {
                UIGlobals.Do.RebuildViews(null, isNewDay);
            }

            UIGlobals.Do.ShowHideIdleMode(idle);
        }

        public void ShowToasterNotification(string message, bool extraTime)
        {
            //create on first use
            if (LongToaster == null)
            {
                LongToaster = new Toast.Notifier(cfg =>
                {
                    cfg.PositionProvider = new ToastP.PrimaryScreenPositionProvider(ToastP.Corner.BottomRight, 10, 10);
                    cfg.LifetimeSupervisor = new ToastL.TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(120), ToastL.MaximumNotificationCount.FromCount(8));
                    cfg.Dispatcher = Application.Current.Dispatcher;
                    cfg.DisplayOptions.TopMost = true;
                    cfg.DisplayOptions.Width = 600;
                });
            }
            if (ShortToaster == null)
            {
                ShortToaster = new Toast.Notifier(cfg =>
                {
                    cfg.PositionProvider = new ToastP.PrimaryScreenPositionProvider(ToastP.Corner.BottomRight, 10, 10);
                    cfg.LifetimeSupervisor = new ToastL.TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(12), ToastL.MaximumNotificationCount.FromCount(8));
                    cfg.Dispatcher = Application.Current.Dispatcher;
                    cfg.DisplayOptions.TopMost = true;
                    cfg.DisplayOptions.Width = 600;
                });
            }

            message += " (Systematizer)";
            var options = new ToastC.MessageOptions { FontSize = 15 };
            if (extraTime)
                ToastM.InformationExtensions.ShowInformation(LongToaster, message, options);
            else
                ToastM.InformationExtensions.ShowInformation(ShortToaster, message, options);

            SystemSounds.Exclamation.Play();
        }
    }
}
