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
        TextBox_LostFocus(null, null);
    }

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        //does not work: ((TextBox)sender).SelectAll();
    }

    //arguments ignored
    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        VisualUtils.DelayThen(1, () =>
        {
            eTime.SelectAll(); //so that when you go back to it, it is all selected
        });
    }
}
