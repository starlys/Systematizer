using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF;

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

    void TreeView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            VM.OpenRequested?.Invoke();
    }

    private void TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not TreeView t) return;
        if (e.Handled) return;
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };
        var parent = t.Parent as UIElement;
        parent.RaiseEvent(eventArg);
    }
}
