using System;
using System.Windows;
using System.Windows.Threading;

namespace Systematizer.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var c = new MainController();
            UIGlobals.Do = c;
            c.Initialize(this);

            //DEBUG
            //var timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(4) };
            //timer.Tick += (s, e) => UIGlobals.CommonActions.ShowToasterNotification("test debug", false);
            //timer.Start();
        }
    }
}
