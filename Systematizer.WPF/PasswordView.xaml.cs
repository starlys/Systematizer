using System;
using System.Windows;
using System.Windows.Controls;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for PasswordView.xaml
    /// </summary>
    public partial class PasswordView : UserControl
    {
        public PasswordView()
        {
            InitializeComponent();
        }

        PasswordVM VM => DataContext as PasswordVM;

        void Reveal_Click(object sender, RoutedEventArgs e)
        {
            VM.IsRevealed = true;
        }

        void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(VM.Value);
        }
    }
}
