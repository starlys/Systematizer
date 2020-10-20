using System;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class RawEmailVM : EditableVM
    {
        //injected behavior
        public readonly Func<CommandCenter.Item, bool> HandleCommand;

        public RawEmailVM(Func<CommandCenter.Item, bool> handleCommand)
        {
            HandleCommand = handleCommand;
        }

        string _value;
        public string Value
        {
            get => _value;
            set
            {
                _value = value; 
                NotifyChanged(); 
                NotifyChanged("HasValue");
                NotifyChanged("IsViewVisible");
            }
        }

        public bool HasValue => !string.IsNullOrEmpty(_value);

        public Visibility IsViewVisible => ToVisibility(HasValue);
    }
}
