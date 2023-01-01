using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for CalendarView.xaml
/// </summary>
public partial class CalendarView : UserControl
{
    CalendarVM VM => DataContext as CalendarVM;

    public CalendarView()
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

    void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        VM.ControlResized?.Invoke(e.NewSize.Width - 2);
    }

    void Bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var barvm = ((Border)sender).DataContext as CalendarVM.BarVM;
        VM.OpenRequested?.Invoke(barvm);
    }
}
