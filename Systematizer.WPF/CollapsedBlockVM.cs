namespace Systematizer.WPF;

class CollapsedBlockVM : BaseBlockVM
{
    public string Title { get; set; }

    //injected behavior
    public Action ExpansionRequested; 

    public CollapsedBlockVM(Action<BaseBlockVM> vmGotFocusHandler) : base(vmGotFocusHandler)
    { }

    public override string BlockTitle => "";
}
