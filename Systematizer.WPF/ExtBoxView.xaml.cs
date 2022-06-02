using System;
using System.Windows.Controls;
using System.Windows.Input;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for BoxView.xaml
    /// </summary>
    public partial class ExtBoxView : UserControl
    {
        ExtBoxVM VM => DataContext as ExtBoxVM;

        public ExtBoxView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                if (VM == null) return;
                VM.GetMainControl = () =>
                {
                    return VisualUtils.GetByUid(this, "eTitle") as TextBox;
                };
            };
        }

        void ClassifyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            VM.HandleCommand?.Invoke(Globals.Commands.CLASSIFY);
        }

        void Title_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && VM.IsEditMode && !IsCtrlDown)
            {
                if (VM.BoxTime_DateVisibility == System.Windows.Visibility.Visible)
                    FocusDate();
                else
                    //date not visible: go to notes
                    FocusNotes();
                e.Handled = true;
            }
        }

        void Date_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && VM.IsEditMode && !IsCtrlDown)
            {
                FocusNotes();
                e.Handled = true;
            }
        }

        void FocusDate()
        {
            var ctrl = VisualUtils.GetByUid(this, "eDate") as DateView;
            VisualUtils.DelayThen(20, () => ctrl?.FocusMainControl());
        }

        void FocusNotes()
        {
            var ctrl = VisualUtils.GetByUid(this, "eNotes") as RichTextView;
            VisualUtils.DelayThen(20, () => ctrl?.FocusMainControl());
        }

        static bool IsCtrlDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
    }
}
