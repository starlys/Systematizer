using System;
using System.Collections.ObjectModel;

namespace Systematizer.WPF
{
    /// <summary>
    /// viewmodel for whole screen
    /// </summary>
    class WholeVM
    {
        public class Stack
        {
            public ObservableCollection<BaseBlockVM> Blocks { get; set; } = new ObservableCollection<BaseBlockVM>();
        }

        public Stack HomeStack { get; set; } = new Stack();
        public Stack EditStack { get; set; } = new Stack();
    }
}
