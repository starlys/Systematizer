using System;
using System.Windows;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for DateTimeDialog.xaml
    /// </summary>
    public partial class DateTimeDialog : Window
    {
        public DateTimeDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Update or enter a date; returns null on cancel
        /// </summary>
        /// <param name="defaultDate">null ok</param>
        public static string GetDateOnly(string defaultDate)
        {
            var vm = new DateVM { Date = defaultDate };
            var dlg = new DateTimeDialog { DataContext = vm };
            if (dlg.ShowDialog() == true) return vm.Date;
            return null;
        }
    }
}
