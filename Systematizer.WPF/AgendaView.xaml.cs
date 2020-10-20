using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for AgendaView.xaml
    /// </summary>
    public partial class AgendaView : UserControl
    {
        AgendaVM VM => DataContext as AgendaVM;

        public AgendaView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                if (VM == null) return;
                VM.GetMainControl = () =>
                {
                    return VisualUtils.GetByUid(this, "eFocusBar") as Button;
                };
            };
        }

        ItemsControl Rows => VisualUtils.GetByUid(this, "eRows") as ItemsControl;

        void Title_GotFocus(object sender, RoutedEventArgs e)
        {
            var rowIdx = VisualUtils.IndexOfControlInItemsControl(Rows, (DependencyObject)sender);
            if (rowIdx >= 0)
                VM.ItemGotFocus?.Invoke(rowIdx);
            ((TextBox)sender).SelectAll();
        }

        void More_Click(object sender, RoutedEventArgs e)
        {
            VM.MoreRequested?.Invoke();
        }

        void Time_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ctrl = (FrameworkElement)sender;
            var rowIdx = VisualUtils.IndexOfControlInItemsControl(Rows, ctrl);
            VM.MouseOpenRequested?.Invoke(ctrl, VM.Rows[rowIdx]);
        }
    }
}
