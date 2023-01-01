using System.Windows.Controls;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for DateView.xaml
/// </summary>
public partial class DateView : UserControl
{
    DateVM VM => DataContext as DateVM;

    public DateView()
    {
        InitializeComponent();
    }

    public void FocusMainControl()
    {
        eInstaChange.Focus();
    }

    void CalendarToggle_Click(object sender, RoutedEventArgs e)
    {
        bool isOpen = eToggle.IsChecked == true;
        if (isOpen)
        {
            var current = DateUtil.ToDateTime(VM.Date);
            if (current.HasValue)
            {
                eCalendar.SelectedDate = current.Value;
                eCalendar.DisplayDate = current.Value;
            }
            UIGlobals.WindowAffectsPopupAction = () => eToggle.IsChecked = false;
        }
        else
        {
            UIGlobals.WindowAffectsPopupAction = null;
        }
        VM.IsCalendarOpen = isOpen;
    }

    private void Win_LocationChanged(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        if (eCalendar.SelectedDate == null) return;
        string d = DateUtil.ToYMD(eCalendar.SelectedDate.Value);
        VM.Date = d;
        eToggle.IsChecked = false;
        VM.IsCalendarOpen = false;
    }

#pragma warning disable IDE1006 // Naming Styles
    async void eInstaChange_LostFocus(object sender, RoutedEventArgs e)
    {
        await Task.Delay(100);
        VM.InstaChange = "";
    }
#pragma warning restore IDE1006 // Naming Styles

    //this is called on any UI event outside the popup, because StaysOpen=false
    private void Popup_Closed(object sender, EventArgs e)
    {
        eToggle.IsChecked = false;
        //CalendarToggle_Click(null, null);
    }
}
