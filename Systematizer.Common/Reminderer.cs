using System;
using System.Linq;

namespace Systematizer.Common
{
    /// <summary>
    /// Helpers for sending OS toaster reminders
    /// </summary>
    static class Reminderer
    {
        public static void CheckAndSend()
        {
            DateTime now = DateTime.Now.AddSeconds(-30); //be proactive 
            DateTime tooOld = now.AddMinutes(-15);
            var agenda = Globals.BoxCache?.GetAgenda();
            if (agenda == null) return;
            foreach (var ag in agenda.Where(r => r.PendingPrepReminder != null && r.PendingPrepReminder.Value < now))
            {
                if (ag.PendingPrepReminder.Value > tooOld) SendOne(ag.Box, true);
                ag.PendingPrepReminder = null;
            }
            foreach (var ag in agenda.Where(r => r.PendingReminder != null && r.PendingReminder.Value < now))
            {
                if (ag.PendingReminder.Value > tooOld) SendOne(ag.Box, false);
                ag.PendingReminder = null;
            }
        }

        static void SendOne(CachedBox box, bool isPrep)
        {
            string message = isPrep ? $"{box.PrepDuration}: {box.Title}"
                : $"Now: {box.Title}";
            bool extraTime = box.Importance == Constants.IMPORTANCE_HIGH;
            Globals.UIAction.ShowToasterNotification(message, extraTime);
        }

    }
}
