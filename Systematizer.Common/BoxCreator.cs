using Systematizer.Common.PersistentModel;

namespace Systematizer.Common;

/// <summary>
/// Contains presets for new boxes. The indexes of NAMES are used in the call to Get
/// </summary>
public static class BoxCreator
{
    public static readonly string[] NAMES = { "Task", "Appointment", "Optional event", "Reminder", "Trip" };

    public const int TASK_PRESET_NO = 0;
    
    public static Box GetPreset(int presetNo)
    {
        var box = new Box
        {
            TimeType = Constants.TIMETYPE_MINUTE,
            Importance = Constants.IMPORTANCE_NORMAL,
            Visibility = Constants.VISIBILITY_NORMAL
        };

        if (presetNo == 0) //task
        {
            box.TimeType = Constants.TIMETYPE_DAY;
        }
        if (presetNo == 1) //appt
        {
            box.Visibility = Constants.VISIBILITY_PLANAROUND;
            box.Duration = "1h";
            box.PrepDuration = "15m";
        }
        if (presetNo == 2) //optional event
        {
            box.Visibility = Constants.VISIBILITY_LOWCLUTTER;
            box.Duration = "1h";
            box.PrepDuration = "15m";
        }
        if (presetNo == 3) //reminder
        {
            box.TimeType = Constants.TIMETYPE_DAY;
            box.Visibility = Constants.VISIBILITY_LOWCLUTTER;
        }
        if (presetNo == 4) //trip
        {
            box.TimeType = Constants.TIMETYPE_DAY;
            box.Visibility = Constants.VISIBILITY_HIGHLIGHT;
        }

        return box;
    }

    public static Box GetUnclassified()
    {
        return new Box
        {
            IsUnclass = 1,
            Importance = Constants.IMPORTANCE_NORMAL,
            Visibility = Constants.VISIBILITY_NORMAL
        };
    }
}
