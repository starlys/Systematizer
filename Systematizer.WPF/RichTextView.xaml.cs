using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

#pragma warning disable IDE1006 // Naming Styles

namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for RichTextView.xaml
/// </summary>
public partial class RichTextView : UserControl
{
    RichTextVM VM => DataContext as RichTextVM;

    public RichTextView()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            //remove key bindings except A, C, X, V
            for (Key k = Key.A; k <= Key.Z; ++k)
            {
                if (k == Key.C || k == Key.V || k == Key.A || k == Key.X) continue;
                AddNoopCtrlKeyBinding(k);
            }
            DataObject.AddPastingHandler(eRTB, new DataObjectPastingEventHandler(OnPaste));
            VM?.Initialize(eRTB);
        };
        
    }

    public void FocusMainControl()
    {
        eRTB.Focus();
    }

    void eRTB_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        VM?.UpdateText();
    }
    
    void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText)) return;
        if (e.SourceDataObject.GetData(DataFormats.UnicodeText) is string s)
            Clipboard.SetText(s);
    }

    void CommandBinding_Disabled(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = false;
        e.Handled = false;
    }

    void eRTB_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            VM?.EnterPressed();
    }

    void AddNoopCtrlKeyBinding(Key k)
    {
        eRTB.InputBindings.Add(new KeyBinding(ApplicationCommands.NotACommand, k, ModifierKeys.Control));
    }

    void Hyperlink_Click(object sender, MouseEventArgs e)
    {
        //this is a better solution because it would allow left button to work, but it is not being called
        var hlink = (Hyperlink)sender;
        try { Process.Start(hlink.NavigateUri.ToString()); }
        catch { }
    }
}
