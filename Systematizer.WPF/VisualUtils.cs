using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Systematizer.WPF;

static class VisualUtils
{
    /// <summary>
    /// Open a file, such as a Word file using the default app
    /// </summary>
    public static void OpenWithWithDefaultApp(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer",
            Arguments = $"\"{path}\""
        });
    }

    public static void ComposeEmailTo(string emailAddress)
    {
        try
        {
            //Process.Start("mailto:" + emailAddress);
            //System.Runtime.InteropServices.she
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "mailto:" + emailAddress,
            });
        }
        catch (Exception ex)
        {
            ShowMessageDialog("Email failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Advance focus (as if user pressed tab)
    /// </summary>
    public static void FocusNext()
    {
        if (Keyboard.FocusedElement is Control ctrl)
        {
            ctrl.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    /// <summary>
    /// Make the current control lose then regain focus; use this to ensure binding is completed in the focused control
    /// </summary>
    public static void LoseRegainFocus()
    {
        if (Keyboard.FocusedElement is Control ctrl)
        {
            ctrl.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            ctrl.Focus();
        }
    }

    /// <summary>
    /// Show message with information icon, forcing user to confirm
    /// </summary>
    public static void ShowMessageDialog(string s)
    {
        MessageBox.Show(App.Current.MainWindow, s, "Systematizer", MessageBoxButton.OK, MessageBoxImage.Information); 
    }

    /// <summary>
    /// Show message with OK/Cancel, using question icons; return true if confirmed
    /// </summary>
    public static bool Confirm(string s)
    {
        return MessageBox.Show(s, "Systematizer", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
    }

    /// <summary>
    /// Get the index in an ItemsControl of any visual object rendered by the template, or -1 if not found
    /// </summary>
    public static int IndexOfControlInItemsControl(ItemsControl ic, DependencyObject el)
    {
        while (el is not ContentPresenter && el != null)
            el = VisualTreeHelper.GetParent(el);
        if (el == null) return -1;
        return ic.ItemContainerGenerator.IndexFromContainer(el);
    }

    public static void DelayThen(int millis, Action continuation)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(millis) };
        timer.Start();
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            continuation();
        };
    }

    /// <summary>
    /// Find child element by Uid
    /// </summary>
    public static UIElement GetByUid(DependencyObject rootElement, string uid)
    {
        int count = VisualTreeHelper.GetChildrenCount(rootElement);
        for (int i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(rootElement, i) is UIElement el)
            {
                if (el.Uid == uid) return el;
                el = GetByUid(el, uid);
                if (el != null) return el;
            }
        }
        return null;
    }
}
