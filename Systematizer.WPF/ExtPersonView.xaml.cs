using System.Windows.Controls;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for ExtPersonView.xaml
/// </summary>
public partial class ExtPersonView : UserControl
{
    ExtPersonVM VM => DataContext as ExtPersonVM;

    public ExtPersonView()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (VM == null) return;
            VM.GetMainControl = () =>
            {
                return VisualUtils.GetByUid(this, "eName") as TextBox;
            };
        };
    }

    void Categories_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        UIGlobals.Do.HandleGlobalCommand(Globals.Commands.EDITCATEGORIES);
    }
}
