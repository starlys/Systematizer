using System;
using System.Windows;
using System.Windows.Controls;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for BoxSearchView.xaml
    /// </summary>
    public partial class BoxSearchView : UserControl
    {
        BoxSearchVM VM => DataContext as BoxSearchVM;

        public BoxSearchView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                if (VM == null) return;
                VM.GetMainControl = () =>
                {
                    return VisualUtils.GetByUid(this, "eTerm") as TextBox;
                };
                VM.GetPreResultsControl = () =>
                 {
                     return VisualUtils.GetByUid(this, "eSearch") as Button;
                 };
            };
        }

        void Search_Click(object sender, RoutedEventArgs e)
        {
            VM.SearchRequested();
        }

        private void Search_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                VM.SearchRequested();
                e.Handled = true;
            }
        }
    }
}
