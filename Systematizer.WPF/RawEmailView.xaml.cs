using System;
using System.Windows;
using System.Windows.Controls;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for RawEmailView.xaml
    /// </summary>
    public partial class RawEmailView : UserControl
    {
        RawEmailVM VM => DataContext as RawEmailVM;

        public RawEmailView()
        {
            InitializeComponent();
        }

        void View_Click(object sender, RoutedEventArgs e)
        {
            VM.HandleCommand?.Invoke(Globals.Commands.VIEWEMAIL);
        }

        void Capture_Click(object sender, RoutedEventArgs e)
        {
            VM.HandleCommand?.Invoke(Globals.Commands.CAPTUREEMAIL);
        }
    }
}
