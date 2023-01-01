using System.Windows.Controls;

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for SelectDialog.xaml
/// </summary>
public partial class SelectDialog : Window
{
    class ItemVM
    {
        public string Text { get; set; }
        public int Index { get; set; }
    }

    int SelectedIndex = -1;

    public SelectDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows dialog asking user to select one of the items; returns index of -1 if canceled
    /// </summary>
    public static int SelectFromList(List<string> items)
    {
        //build vm and add accelerator keys 1..9
        var vm = items.Select(s => new ItemVM { Text = s }).ToArray();
        for (int i = 0; i < vm.Length; ++i)
        {
            vm[i].Index = i;
            if (i < 9) vm[i].Text = $"_{i + 1} {items[i]}";
        }

        var dlg = new SelectDialog
        {
            Owner = App.Current.MainWindow
        };
        dlg.eList.ItemsSource = vm;
        dlg.ShowDialog();
        return dlg.SelectedIndex;
    }

    void Button_Click(object sender, RoutedEventArgs e)
    {
        SelectedIndex = (int)((Button)e.Source).Tag;
        DialogResult = true;
    }
}
