using System.Windows.Controls;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for BlockLinkView.xaml
/// </summary>
public partial class BlockLinkView : UserControl
{
    public BlockLinkView()
    {
        InitializeComponent();
    }

    void Link_Click(object sender, RoutedEventArgs e)
    {
        if (((TextBlock)sender).DataContext is BlockLinkVM.ItemVM vm)
            vm.LinkClicked?.Invoke(vm);
    }
}
