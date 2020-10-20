using System;
using System.Windows;
using System.Windows.Controls;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for RepeatInfoView.xaml
    /// </summary>
    public partial class RepeatInfoView : UserControl
    {
        RepeatInfoVM VM => DataContext as RepeatInfoVM;

        public RepeatInfoView()
        {
            InitializeComponent();
        }

        void RemoveEntry_Click(object sender, RoutedEventArgs e)
        {
            int itemIdx = VisualUtils.IndexOfControlInItemsControl(eEntries, (Button)sender);
            if (itemIdx >= 0)
                VM.Entries.RemoveAt(itemIdx);
        }

        void EditEntry_Click(object sender, RoutedEventArgs e)
        {
            int itemIdx = VisualUtils.IndexOfControlInItemsControl(eEntries, (Button)sender);
            if (itemIdx >= 0)
            {
                var entry = RepeatPatternDialog.ShowDialog(VM.Entries[itemIdx].Entry);
                if (entry != null)
                {
                    VM.Entries.RemoveAt(itemIdx);
                    VM.Entries.Insert(itemIdx, new RepeatInfoVM.EntryVM(entry));
                }
            }
        }

        void AddPattern_Click(object sender, RoutedEventArgs e)
        {
            var pat = new ParsedRepeatInfo.RepeatEntry()
            {
                Arg2 = new[] { true, true, true, true, true },
                Kind = ParsedRepeatInfo.RepeatKind.WeekOfMonth
            };
            var entry = RepeatPatternDialog.ShowDialog(pat);
            if (entry != null) VM.Entries.Add(new RepeatInfoVM.EntryVM(entry));
        }

        void AddException_Click(object sender, RoutedEventArgs e)
        {
            var toAdd = RepeatExceptionDialog.MakeException(VM);
            foreach (var entry in toAdd)
                VM.Entries.Add(new RepeatInfoVM.EntryVM(entry));
        }
    }
}
