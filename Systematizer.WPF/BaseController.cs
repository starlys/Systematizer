namespace Systematizer.WPF;

abstract class BaseController
{
    public enum Mode { ReadOnly, Edit }

    /// <summary>
    /// Switch between collapse/read/edit mode and optionally save changes.
    /// Block controllers don't support collapsed mode so they can treat it like readonly mode; the collapsed mode
    /// is handled at the block-stack level.
    /// </summary>
    /// <returns>true if ok, false if saving failed</returns>
    public abstract bool ChangeMode(Mode mode, bool saveChanges);

    /// <summary>
    /// Handle commands; returns true if handled at this level
    /// </summary>
    public virtual bool HandleCommand(CommandCenter.Item command) { return false; }
}

abstract class BlockController : BaseController
{
    abstract public BaseBlockVM GenericVM { get; }

    protected Action<BlockController> BlockGotFocus { get; private set; }

    /// <summary>
    /// handle collapse; 2nd argument true means to remove completely
    /// </summary>
    protected Action<BlockController, bool> CollapseRequested { get; private set; }

    public BlockController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested)
    {
        BlockGotFocus = blockGotFocusHandler;
        CollapseRequested = collapseRequested;
    }

    protected void VMGotFocus(BaseBlockVM _) 
    {
        BlockGotFocus(this);
        UIGlobals.RecordLinkController.BlockActivated(this);
    }

    public void LoseFocus()
    {
        GenericVM.HasBlockFocus = false;
    }

    public override bool HandleCommand(CommandCenter.Item command)
    {
        if (command == Globals.Commands.CLOSE)
        {
            CollapseRequested(this, false);
            return true;
        }
        return false;
    }

    public virtual void AfterReinflated() { }
}

abstract class ListBlockController : BlockController
{
    protected ListBlockController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested) : 
        base(blockGotFocusHandler, collapseRequested) { }

    /// <summary>
    /// Efficiently refresh/rebuild viewmodels if changes detected; can be called often
    /// </summary>
    /// <param name="changes">can be null if this is called because of elapsed time</param>
    public abstract void Refresh(BoxEditingPool.Item changes);
}
