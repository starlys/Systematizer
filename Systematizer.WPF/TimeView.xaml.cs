using System;
using System.Windows;
using System.Windows.Controls;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for TimeView.xaml
    /// </summary>
    public partial class TimeView : UserControl
    {
        TimeVM VM => DataContext as TimeVM;

        public TimeView()
        {
            InitializeComponent();
        }

        void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
