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
                VM.GetResultsControl = () =>
                 {
                     return VisualUtils.GetByUid(this, "eResults") as ItemsControl;
                 };
            };
        }

        void Search_Click(object sender, RoutedEventArgs e)
        {
            VM.SearchRequested();
        }
    }
}
