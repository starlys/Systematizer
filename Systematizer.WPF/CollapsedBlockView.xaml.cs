using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for CollapsedBlockView.xaml
/// </summary>
public partial class CollapsedBlockView : UserControl
{
    CollapsedBlockVM VM => DataContext as CollapsedBlockVM;

    public CollapsedBlockView()
    {
        InitializeComponent();
    }

    void Title_MouseDown(object sender, MouseButtonEventArgs e)
    {
        VM.ExpansionRequested?.Invoke();
    }

    void DockPanel_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
    }
}
