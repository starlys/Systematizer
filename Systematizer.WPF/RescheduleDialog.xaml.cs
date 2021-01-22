using System;
using System.Windows;
using System.Windows.Controls;
using Systematizer.Common;

#pragma warning disable IDE1006 // Naming Styles

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for RescheduleDialog.xaml
    /// </summary>
    public partial class RescheduleDialog : Window
    {
        char AdvanceChar = '!';
        const string ALLOWED_ADVANCE_CHARS = "123456789SMTWHFA";

        public RescheduleDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Given a date, allow the user to advance it by number of days or to the next DOW.
        /// Inputs and outputs in YYYYMMDD format.
        /// </summary>
        /// <returns>null if canceled</returns>
        public static string ShowDialog(string date)
        {
            var dlg = new RescheduleDialog
            {
                Owner = App.Current.MainWindow
            };
            dlg.Loaded += (s, e) => dlg.eCommand.Focus();
            if (dlg.ShowDialog() != true) return null;
            return DateUtil.AdvanceByShortcutKey(date, dlg.AdvanceChar);
        }

        void eCommand_TextChanged(object sender, TextChangedEventArgs e)
        {
            string s = eCommand.Text;
            if (string.IsNullOrEmpty(s)) return;
            char advance = char.ToUpperInvariant(s[0]);
            if (!ALLOWED_ADVANCE_CHARS.Contains(advance)) return;
            AdvanceChar = advance;
            DialogResult = true;
        }
    }
}
