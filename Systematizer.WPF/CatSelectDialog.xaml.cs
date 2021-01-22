using System;
using System.Collections.Generic;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for CatSelectDialog.xaml
    /// </summary>
    public partial class CatSelectDialog : Window
    {
        CatCache.Item SelectedCat => (eTree.SelectedItem) as CatCache.Item;

        class VM : BaseVM
        {
            public VM()
            {
                Roots = Globals.AllCats.Roots;
            }

            public List<CatCache.Item> Roots { get; set; }
        }

        public CatSelectDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Get a category; return null if canceled
        /// </summary>
        public static CatCache.Item SelectCat(string caption)
        {
            var dialog = new CatSelectDialog
            {
                Owner = App.Current.MainWindow,
                DataContext = new VM()
            };
            dialog.eCaption.Text = caption;
            if (dialog.ShowDialog() != true) return null;
            return dialog.SelectedCat;
        }

        void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
