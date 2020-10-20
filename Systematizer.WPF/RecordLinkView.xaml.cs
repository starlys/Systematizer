using System;
using System.Windows;
using System.Windows.Controls;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for RecordLinkView.xaml
    /// </summary>
    public partial class RecordLinkView : UserControl
    {
        RecordLinkVM VM => DataContext as RecordLinkVM;

        public RecordLinkView()
        {
            InitializeComponent();
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            int idx = VisualUtils.IndexOfControlInItemsControl(eItems, (Button)sender);
            if (idx >= 0)
                VM?.ActionRequested?.Invoke(VM.Items[idx]);
        }

        void Done_Click(object sender, RoutedEventArgs e)
        {
            VM.IsActive = false;
        }
    }
}
