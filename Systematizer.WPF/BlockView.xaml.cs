using System;
using System.Windows;
using System.Windows.Controls;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for BlockView.xaml
    /// </summary>
    public partial class BlockView : UserControl
    {
        BaseBlockVM VM => DataContext as BaseBlockVM;

        public BlockView()
        {
            InitializeComponent();
        }

        void DockPanel_GotFocus(object sender, RoutedEventArgs e)
        {
            VM?.BlockGotFocus?.Invoke(VM);
        }

        void FocusBar_Click(object sender, RoutedEventArgs e)
        {
            VM?.FocusBarClicked?.Invoke();
        }

        void Close_Click(object sender, RoutedEventArgs e)
        {
            VM?.CloseClicked?.Invoke(VM);
        }
    }
}
