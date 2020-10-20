using System;
using System.Windows;
using System.Windows.Controls;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for BlockLinkView.xaml
    /// </summary>
    public partial class BlockLinkView : UserControl
    {
        public BlockLinkView()
        {
            InitializeComponent();
        }

        void Link_Click(object sender, RoutedEventArgs e)
        {
            var vm = ((TextBlock)sender).DataContext as BlockLinkVM.ItemVM;
            if (vm != null)
                vm.LinkClicked?.Invoke(vm);
        }
    }
}
