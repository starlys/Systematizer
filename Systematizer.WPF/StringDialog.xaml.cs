namespace Systematizer.WPF;

/// <summary>
/// Dialog to enter text
/// </summary>
public partial class StringDialog : Window
{
    public StringDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Get input; return null on cancel
    /// </summary>
    public static string GetInput(string caption, string initialValue, int maxLength)
    {
        var dialog = new StringDialog
        {
            Owner = Application.Current.MainWindow
        };
        dialog.eCaption.Text = caption;
        dialog.eValue.MaxLength = maxLength;
        dialog.eValue.Text = initialValue;
        if (dialog.ShowDialog() != true) return null;
        return dialog.eValue.Text;
    }

    void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
