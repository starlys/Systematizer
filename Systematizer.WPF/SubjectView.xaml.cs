using System;
using System.Windows;
using System.Windows.Controls;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for SubjectView.xaml
    /// </summary>
    public partial class SubjectView : UserControl
    {
        SubjectVM VM => DataContext as SubjectVM;

        public SubjectView()
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

        void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is SubjectVM.RowVM rowVM)
                VM.ItemGotFocus?.Invoke(rowVM);
        }

        void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            if (((TreeViewItem)e.OriginalSource).DataContext is SubjectVM.RowVM rowVM)
                VM.ItemExpanded?.Invoke(rowVM, true);
        }

        void TreeView_Collapsed(object sender, RoutedEventArgs e)
        {
            if (((TreeViewItem)e.OriginalSource).DataContext is SubjectVM.RowVM rowVM)
                VM.ItemExpanded?.Invoke(rowVM, false);
        }

        void TreeView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                VM.OpenRequested?.Invoke();
        }
    }
}
