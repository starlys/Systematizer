using System.Windows.Controls;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for TodayView.xaml
/// </summary>
public partial class TodayView : UserControl
{
    TodayVM VM => DataContext as TodayVM;

    static TodayVM.ChunkVM LastDropTarget;

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

    void ChunkTitle_DragEnter(object sender, DragEventArgs e)
    {
        bool ok = e.Data.GetDataPresent(nameof(BoxDragInfo));
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
        bool ok = e.Data.GetDataPresent(nameof(BoxDragInfo));
        var chunkIdx = VisualUtils.IndexOfControlInItemsControl(ChunkList, (DependencyObject)sender);
        if (chunkIdx == -1) ok = false;
        if (!ok) return;
        var di = e.Data.GetData(nameof(BoxDragInfo)) as BoxDragInfo;
        VM.DropOnChunkRequested(di, VM.Chunks[chunkIdx]);
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
