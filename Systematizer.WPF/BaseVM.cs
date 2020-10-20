using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Base class for view models. Each implementation for a principal entity should own the underlying persistent object and implement
    /// the two methods to map values to/from that to the bindable VM properties.
    /// Subclasses for parts of entities can omit the methods and the owning VM has to handle the load/save.
    /// </summary>
    abstract class BaseVM : INotifyPropertyChanged 
    {
        static BooleanToVisibilityConverter BooleanToVisibility = new BooleanToVisibilityConverter();

        bool _isDirty;
        public virtual bool IsDirty
        {
            get => _isDirty; 
            set { _isDirty = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Set the bindable VM fields from the persistent version; this should be called in ctor, but you can call
        /// at other times to undo edits.
        /// </summary>
        public virtual void InitializeFromPersistent() { }

        /// <summary>
        /// Prep persistent
        /// </summary>
        public virtual void WriteToPersistent() { }

        protected void NotifyChanged([CallerMemberName]string propertyName = "")
        {
            IsDirty = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected Visibility ToVisibility(bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// A VM for the top level blocks showing in either stack
    /// </summary>
    abstract class BaseBlockVM : BaseVM
    {
        /// <summary>
        /// Called by user focus only; controller should handle by setting properties (but not refocus which could cause a race)
        /// </summary>
        public Action<BaseBlockVM> BlockGotFocus { get; private set; }

        /// <summary>
        /// Called by code-behind and handled by controller 
        /// </summary>
        public Action FocusBarClicked{ get; set; }
        
        public Action<BaseBlockVM> CloseClicked { get; private set; } 

        /// <summary>
        /// Optionally implemented by code behind to return the main entry control
        /// </summary>
        public Func<FrameworkElement> GetMainControl { get; set; }

        public BaseBlockVM(Action<BaseBlockVM> gotFocusAction)
        {
            BlockGotFocus = gotFocusAction;
            CloseClicked = vm => UIGlobals.Do.HandleGlobalCommand(Globals.Commands.CLOSE);
        }

        public abstract string BlockTitle { get; }

        bool _hasBlockFocus;
        public bool HasBlockFocus
        {
            get => _hasBlockFocus;
            set
            {
                _hasBlockFocus = value;
                NotifyChanged();
                NotifyChanged("FocusBarColor");
            }
        }

        public Brush FocusBarColor => HasBlockFocus ? Brushes.Orange : Brushes.Silver;

        public virtual Brush BlockBackground => Brushes.Silver;
    }

    /// <summary>
    /// A VM base class for blocks that contains lists of things (such as today, agenda, search results)
    /// </summary>
    abstract class BaseListBlockVM : BaseBlockVM
    {
        public BaseListBlockVM(Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
        {
        }
    }

    /// <summary>
    /// A VM for blocks that support edit and read modes for single persistent records
    /// </summary>
    abstract class BaseEditableBlockVM : BaseBlockVM
    {
        public BaseEditableBlockVM(Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
        {
        }

        bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                NotifyChanged();
                NotifyChanged("IsReadOnly");
                NotifyChanged("BlockBackground");
                NotifyChanged("EditModeVisibility");
                EditModeChanged();
            }
        }

        public bool IsReadOnly => !IsEditMode;

        /// <summary>
        /// Visibility binding (visible when editable)
        /// </summary>
        public Visibility EditModeVisibility => IsEditMode ? Visibility.Visible : Visibility.Collapsed;

        public override Brush BlockBackground => IsEditMode ? Brushes.White : Brushes.Silver;

        /// <summary>
        /// When overridden in a VM, can be used to propagate change notification to other properties
        /// </summary>
        protected virtual void EditModeChanged() { }
    }

    /// <summary>
    /// BaseVM with IsEditMode and IsReadOnly property (used for detail components, not whole blocks)
    /// </summary>
    abstract class EditableVM : BaseVM
    {
        bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                NotifyChanged();
                NotifyChanged("IsReadOnly");
                NotifyChanged("EditModeVisibility");
                EditModeChanged();
            }
        }

        public bool IsReadOnly => !IsEditMode;

        /// <summary>
        /// Visibility binding (visible when editable)
        /// </summary>
        public Visibility EditModeVisibility => IsEditMode ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// When overridden in a VM, can be used to propagate change notification to other properties
        /// </summary>
        protected virtual void EditModeChanged() { }
    }
}
