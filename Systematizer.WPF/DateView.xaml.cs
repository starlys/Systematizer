using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Systematizer.Common;

namespace Systematizer.WPF
{
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
            }
            VM.IsCalendarOpen = isOpen;
        }

        void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (eCalendar.SelectedDate == null) return;
            string d = DateUtil.ToYMD(eCalendar.SelectedDate.Value);
            VM.Date = d;
            eToggle.IsChecked = false;
            VM.IsCalendarOpen = false;
        }

        async void eInstaChange_LostFocus(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            VM.InstaChange = "";
        }
    }
}
