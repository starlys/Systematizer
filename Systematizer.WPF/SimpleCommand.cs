using System.Windows.Input;

#pragma warning disable CS0067 //event is never used

namespace Systematizer.WPF;

class SimpleCommand : ICommand
{
    readonly Action<object> Handler;
    public event EventHandler CanExecuteChanged;

    public SimpleCommand(Action<object> handler)
    {
        Handler = handler;
    }

    public bool CanExecute(object parameter) => true;

    public void Execute(object parameter)
    {
        Handler(parameter);
    }
}
