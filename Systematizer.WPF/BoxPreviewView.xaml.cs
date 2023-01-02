using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for BoxPreviewView.xaml
/// </summary>
public partial class BoxPreviewView : UserControl
{
    static BoxPreviewVM LastDropTarget;

    BoxPreviewVM VM => DataContext as BoxPreviewVM;
    Point DragStartPos;
    bool DragPending;

    public BoxPreviewView()
    {
        InitializeComponent();
    }

    void Title_GotFocus(object sender, RoutedEventArgs e)
    {
        VM?.GotFocus?.Invoke(VM);
        ((TextBox)sender).SelectAll();
    }

    void Time_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragStartPos = e.GetPosition(null);
        DragPending = true;
        VM.TimeClicked?.Invoke(VM, (FrameworkElement)sender);
        UIGlobals.LastActivityUtc = DateTime.UtcNow;
    }

    void Time_MouseMove(object sender, MouseEventArgs e)
    {
        //maybe start drag
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var curPos = e.GetPosition(null);
        Vector diff = DragStartPos - curPos;
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            DragPending = false;
            VM.DragStartRequested?.Invoke(VM, (FrameworkElement)sender);
        }
    }

    void Time_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DragPending)
        {
            DragPending = false;
            VM.MouseOpenRequested?.Invoke(VM);
        }
    }

    void TimeDragging_GiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if ((e.Effects & DragDropEffects.Move) != 0)
        {
            e.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }
        else
            e.UseDefaultCursors = true;
    }

    void Box_DragEnter(object sender, DragEventArgs e)
    {
        bool ok = e.Data.GetDataPresent(nameof(BoxDragInfo));
        if (ok) SetDropTarget(VM);
        e.Effects = ok ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    void Box_DragLeave(object sender, DragEventArgs e)
    {
        SetDropTarget(null);
    }

    void Box_Drop(object sender, DragEventArgs e)
    {
        bool ok = e.Data.GetDataPresent(nameof(BoxDragInfo));
        if (!ok) return;
        var di = e.Data.GetData(nameof(BoxDragInfo)) as BoxDragInfo;
        VM.DropUnderBoxRequested(di, VM);
        SetDropTarget(null);
    }

    static void SetDropTarget(BoxPreviewVM vm)
    {
        if (LastDropTarget != null)
        {
            LastDropTarget.DragTargetColor = UIGlobals.TRANSPARENT_BRUSH;
            LastDropTarget = null;
        }
        if (vm != null)
        {
            vm.DragTargetColor = UIGlobals.DRAG_TARGET_BRUSH;
            LastDropTarget = vm;
        }
    }
}
