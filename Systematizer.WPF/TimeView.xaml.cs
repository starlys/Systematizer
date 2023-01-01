using System.Windows.Controls;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for TimeView.xaml
/// </summary>
public partial class TimeView : UserControl
{
    public TimeView()
    {
        InitializeComponent();
    }

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        ((TextBox)sender).SelectAll();
    }
}
