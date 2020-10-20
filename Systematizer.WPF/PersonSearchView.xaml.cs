using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for PersonSearchView.xaml
    /// </summary>
    public partial class PersonSearchView : UserControl
    {
        PersonSearchVM VM => DataContext as PersonSearchVM;

        public PersonSearchView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                if (VM == null) return;
                VM.GetMainControl = () =>
                {
                    return VisualUtils.GetByUid(this, "eTerm") as TextBox;
                };
                VM.GetPreResultsControl= () =>
                {
                    return VisualUtils.GetByUid(this, "eSearch") as Button;
                };
            };
        }

        void Search_Click(object sender, RoutedEventArgs e)
        {
            VM.SearchRequested();
        }

        void Name_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFromLabel((Label)sender);
        }

        void CatFilter_Click(object sender, RoutedEventArgs e)
        {
            var cat = CatSelectDialog.SelectCat("Choose category to include");
            if (cat == null) return;
            VM.CatIdCri = cat.RowId;
            VM.SearchRequested();
        }

        void Clear_Click(object sender, RoutedEventArgs e)
        {
            VM.CatIdCri = null;
            VM.TermCri = "";
            VM.IncludeDetailsCri = false;
            VM.Results.Clear();
        }

        void Result_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenFromLabel((Label)sender);
            }
        }

        void OpenFromLabel(Label ctrl)
        {
            var eResults = VisualUtils.GetByUid(this, "eResults") as ItemsControl;
            int idx = VisualUtils.IndexOfControlInItemsControl(eResults, ctrl);
            if (idx >= 0) VM.OpenRequested(idx);
        }
    }
}
