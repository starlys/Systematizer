using System.Diagnostics;
using System.Reflection;

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
            try
            {
                string versionNo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

                var dlg = new AboutWindow
                {
                    Owner = App.Current.MainWindow
                };
                dlg.eVersion.Text = "Version: " + versionNo;
                dlg.ShowDialog();
            }
            catch { } //crashes app occasionally, don't know why
        }
    }
}
