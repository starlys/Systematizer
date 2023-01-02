using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for TodayView.xaml
/// </summary>
public partial class TodayView : UserControl
{
    TodayVM VM => DataContext as TodayVM;

    static TodayVM.ChunkVM LastDropTarget;

    Point DragStartPos;

    ItemsControl ChunkList => VisualUtils.GetByUid(this, "eChunkList") as ItemsControl;

    public TodayView()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (VM == null) return;
            VM.GetMainControl = () =>
            {
                var chunkView = ChunkList.ItemContainerGenerator.ContainerFromIndex(0);
                if (chunkView == null) return null;
                var chunkTitle = VisualUtils.GetByUid(chunkView, "eChunkTitle") as TextBox;
                return chunkTitle;
            };
        };
    }

    void ChunkTitle_GotFocus(object sender, RoutedEventArgs e)
    {
        var tb = (TextBox)sender;
        tb.SelectAll();
        var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, tb);
        if (chunkIdx >= 0) VM.ChunkGotFocus(chunkIdx);
    }

    void ChunkTitle_LostFocus(object sender, RoutedEventArgs e)
    {
        var tb = (TextBox)sender;
        tb.SelectAll();
        var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, tb);
        if (chunkIdx >= 0) VM.ChunkLostFocus(chunkIdx);
    }

    void ChunkHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragStartPos = e.GetPosition(null);
        UIGlobals.LastActivityUtc = DateTime.UtcNow;
    }

    void ChunkHandle_MouseMove(object sender, MouseEventArgs e)
    {
        //maybe start drag
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var curPos = e.GetPosition(null);
        Vector diff = DragStartPos - curPos;
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (sender is FrameworkElement senderFE)
            {
                var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, senderFE);
                if (chunkIdx >= 0) VM.ChunkDragStartRequested?.Invoke(chunkIdx, (FrameworkElement)sender);
            }
        }
    }

    void ChunkHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
    }

    void ChunkHandleDragging_GiveFeedback(object sender, GiveFeedbackEventArgs e)
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

    //show visual effect for dragging a box or chunk
    void ChunkTitle_DragEnter(object sender, DragEventArgs e)
    {
        bool ok = e.Data.GetDataPresent(nameof(BoxDragInfo)) || e.Data.GetDataPresent(nameof(ChunkDragInfo));
        var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, (DependencyObject)sender);
        if (chunkIdx == -1)
            ok = false;
        else
            SetDropTarget(VM.Chunks[chunkIdx]);
        e.Effects = ok ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    void ChunkTitle_DragLeave(object sender, DragEventArgs e)
    {
        var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, (DependencyObject)sender);
        if (chunkIdx >= 0)
            SetDropTarget(null);
    }

    void ChunkTitle_Drop(object sender, DragEventArgs e)
    {
        var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, (DependencyObject)sender);
        if (chunkIdx == -1) return;

        //drop box
        if (e.Data.GetData(nameof(BoxDragInfo)) is BoxDragInfo di)
            VM.DropBoxOnChunkRequested(di, VM.Chunks[chunkIdx]);

        //drop chunk
        if (e.Data.GetData(nameof(ChunkDragInfo)) is ChunkDragInfo di2)
            VM.DropChunkOnChunkRequested(di2, VM.Chunks[chunkIdx]);

        SetDropTarget(null);
    }

    static void SetDropTarget(TodayVM.ChunkVM vm)
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
