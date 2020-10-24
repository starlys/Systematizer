using System;
using System.Windows.Media;

namespace Systematizer.WPF
{
    static class UIGlobals
    {
        //single instances
        public static IGlobalBehaviors Do;
        public static readonly CommonActions CommonActions = new CommonActions();
        public static readonly DeferredBehaviors Deferred = new DeferredBehaviors();
        public static RecordLinkController RecordLinkController = new RecordLinkController();

        //tracking user activity
        public static DateTime LastActivityUtc = DateTime.UtcNow;

        //constants
        public static readonly Brush[] HIGHLIGHT_COLORS = new[]
        {
            Brushes.DarkTurquoise, Brushes.Yellow, Brushes.LawnGreen, Brushes.Orange, Brushes.Plum, Brushes.LightSlateGray
        };
        public static readonly Brush BOX_TITLE_BRUSH = new SolidColorBrush(Color.FromRgb(240, 255, 255));
        public static readonly Brush DRAG_TARGET_BRUSH = new SolidColorBrush(Colors.Red);
        public static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
    }
}
