using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Systematizer.WPF
{
    /// <summary>
    /// Show version and open source info
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        public static void ShowAbout()
        {
            string versionNo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            var dlg = new AboutWindow
            {
                Owner = App.Current.MainWindow
            };
            dlg.eVersion.Text = "Version: " + versionNo;
            dlg.ShowDialog();
        }
    }
}
