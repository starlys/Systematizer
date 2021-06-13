using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common;

using WinVisibility = System.Windows.Visibility;

namespace Systematizer.WPF
{
    /// <summary>
    /// viewmodel for box detail
    /// </summary>
    class ExtBoxVM : BaseEditableBlockVM
    {
        public override bool IsDirty
        {
            get => base.IsDirty || Password.IsDirty || RawEmail.IsDirty || BoxTime_Date.IsDirty || BoxTime_Time.IsDirty
                || Repeats.IsDirty;
            set
            {
                base.IsDirty = value;
                
                //after save, clear child VMs
                if (!value)
                {
                    Password.IsDirty = RawEmail.IsDirty = BoxTime_Date.IsDirty = BoxTime_Time.IsDirty = Repeats.IsDirty = false;
                }
            }
        }

        public ExtBox Persistent { get; private set; }

        //injected behaviors
        public readonly Func<CommandCenter.Item, bool> HandleCommand;
        public Action FocusMinuteRequested; //injected by code-behind!
        public Action<BlockLinkVM.ItemVM> LinkClicked;

        public ExtBoxVM(ExtBox persistent, Action<BaseBlockVM> gotFocusAction, Func<CommandCenter.Item, bool> handleCommand) : base(gotFocusAction)
        {
            Persistent = persistent;
            HandleCommand = handleCommand;
            InitializeFromPersistent();
            SetBlockTitle();
        }

        string _blockTitle;
        public override string BlockTitle => _blockTitle;

        void SetBlockTitle()
        {
            _blockTitle = (TimeType == Constants.TIMETYPE_NONE ? "Note" : "Task")
                + ": " + Title;
            NotifyChanged(nameof(BlockTitle));
        }

        public override void InitializeFromPersistent()
        {
            BoxTime_Date = new DateVM { Date = Persistent.Box.BoxTime, IsEditMode = IsEditMode };
            BoxTime_Time = new TimeVM { Time = Persistent.Box.BoxTime, IsEditMode = IsEditMode };
            Title = Persistent.Box.Title;
            ParentId = Persistent.Box.ParentId;
            TimeType = Persistent.Box.TimeType;
            Importance = Persistent.Box.Importance;
            Visibility = Persistent.Box.Visibility;
            DoneDate = Persistent.Box.DoneDate;
            IsUnclass = Persistent.Box.IsUnclass != 0;
            Duration = Persistent.Box.Duration;
            PrepDuration = Persistent.Box.PrepDuration;
            Notes = new RichTextVM { Text = Persistent.Box.Notes, IsEditMode = IsEditMode };
            RefDir = Persistent.Box.RefDir;
            RefFile = Persistent.Box.RefFile;
            Password = new PasswordVM { Value = Persistent.Box.Password, IsEditMode = IsEditMode };
            RawEmail = new RawEmailVM(HandleCommand) { Value = Persistent.Box.RawEmail, IsEditMode = IsEditMode };
            Repeats = new RepeatInfoVM(Persistent, () => BoxTime_Time.Time);

            InitializeLinksFromPersistent();

            //needed here because the classify command calls this and some of the properties are based on Persistent.Box properties,
            //so this updates the screen in that case
            NotifyChanged(nameof(ClassifyButtonVisibility));
            NotifyChanged(nameof(ClassificationPanelVisibility));
            NotifyChanged(nameof(BoxTime_DateVisibility));
            NotifyChanged(nameof(BoxTime_TimeVisibility));
        }

        public void InitializeLinksFromPersistent()
        {
            var linkItems = Persistent.Links.Select(l => new BlockLinkVM.ItemVM(l)
            {
                LinkClicked = vm => LinkClicked(vm)
            });
            Links.Items.Clear();
            Links.Items.AddRange(linkItems);
        }

        public override void WriteToPersistent()
        {
            //combine date and time
            string combinedDateTime = null;
            if (BoxTime_Date.Date != null)
            {
                string time = BoxTime_Time.Time ?? DateUtil.DEFAULT_TIME;
                combinedDateTime = BoxTime_Date.Date + time;
                if (combinedDateTime.Length != 12) combinedDateTime = null;
            }

            Persistent.Box.Title = Title;
            Persistent.Box.ParentId = ParentId;
            Persistent.Box.TimeType = TimeType;
            Persistent.Box.Importance = Importance;
            Persistent.Box.Visibility = Visibility;
            Persistent.Box.BoxTime = combinedDateTime;
            Persistent.Box.DoneDate = DoneDate;
            Persistent.Box.IsUnclass = (short)(IsUnclass ? 1 : 0);
            Persistent.Box.Duration = Duration;
            Persistent.Box.PrepDuration = PrepDuration;
            Notes.UpdateText();
            Persistent.Box.Notes = Notes.Text;
            Persistent.Box.RefDir = RefDir;
            Persistent.Box.RefFile = RefFile;
            Persistent.Box.Password = Password.Value;
            Persistent.Box.RawEmail = RawEmail.Value;
            Persistent.Repeats = Repeats.ToParsedRepeatInfo();
        }

        protected override void EditModeChanged()
        {
            BoxTime_Date.IsEditMode = IsEditMode;
            BoxTime_Time.IsEditMode = IsEditMode;
            Password.IsEditMode = IsEditMode;
            RawEmail.IsEditMode = IsEditMode;
            Notes.IsEditMode = IsEditMode;
            Repeats.IsEditMode = IsEditMode;
            NotifyChanged(nameof(ClassifyButtonVisibility));
            if (!IsEditMode) ShowAllChecked = false;
        }

        public static IEnumerable<KeyValuePair<short, string>> TimeTypeOptions => Constants.TIMETYPE_UICHOICES;
        public static IEnumerable<KeyValuePair<short, string>> ImportanceOptions => Constants.IMPORTANCE_UICHOICES;
        public static IEnumerable<KeyValuePair<short, string>> VisibilityOptions => Constants.VISIBILITY_UICHOICES;

        bool _showAllChecked;
        public bool ShowAllChecked
        {
            get => _showAllChecked;
            set
            {
                _showAllChecked = value;
                NotifyChanged();
                NotifyVisibilityDetails();
            }
        }

        public void NotifyVisibilityDetails()
        {
            NotifyChanged(nameof(RefDirVisibility));
            NotifyChanged(nameof(RefFileVisibility));
            NotifyChanged(nameof(PasswordVisibility));
            NotifyChanged(nameof(RawEmailVisibility));
            NotifyChanged(nameof(RepeatsVisibility));
        }

        public WinVisibility RefDirVisibility => ToVisibility(_refDir != null || ShowAllChecked);
        public WinVisibility RefFileVisibility => ToVisibility(_refFile != null || ShowAllChecked);
        public WinVisibility PasswordVisibility => ToVisibility(!string.IsNullOrEmpty(Password.Value) || ShowAllChecked);
        public WinVisibility RawEmailVisibility => ToVisibility(RawEmail.HasValue || ShowAllChecked);
        public WinVisibility RepeatsVisibility => ToVisibility(!IsUnclass && (Repeats.Entries.Count > 0 || ShowAllChecked));
        public WinVisibility ClassifyButtonVisibility => ToVisibility(IsEditMode && Persistent.Box.IsUnclass != 0);
        public WinVisibility ClassificationPanelVisibility => ToVisibility(Persistent.Box.IsUnclass == 0);
        public WinVisibility BoxTime_DateVisibility => ToVisibility(TimeType >= Constants.TIMETYPE_APPROX);
        public WinVisibility DurationVisibility => ToVisibility(TimeType >= Constants.TIMETYPE_DAY);
        public WinVisibility BoxTime_TimeVisibility => ToVisibility(TimeType >= Constants.TIMETYPE_DAY);

        string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                NotifyChanged();
                SetBlockTitle();
            }
        }

        long? _parentId;
        public long? ParentId
        {
            get => _parentId;
            set { _parentId = value; NotifyChanged(); }
        }

        short _timeType;
        public short TimeType
        {
            get => _timeType;
            set
            {
                bool isChangingToMinute = _timeType != Constants.TIMETYPE_MINUTE && value == Constants.TIMETYPE_MINUTE;
                _timeType = value; 
                if (_timeType == Constants.TIMETYPE_NONE)
                {
                    BoxTime_Date.Date = null;
                    BoxTime_Time.Time = null;
                }
                if (_timeType == Constants.TIMETYPE_MINUTE && _visibility < Constants.VISIBILITY_PLANAROUND)
                    Visibility = Constants.VISIBILITY_PLANAROUND;
                NotifyChanged();
                NotifyChanged(nameof(BoxTime_DateVisibility));
                NotifyChanged(nameof(BoxTime_TimeVisibility));
                NotifyChanged(nameof(DurationVisibility));
                SetBlockTitle();
                NotifyChanged(nameof(BlockTitle));
                if (isChangingToMinute) FocusMinuteRequested?.Invoke();
            }
        }

        short _importance;
        public short Importance
        {
            get => _importance;
            set { _importance = value; NotifyChanged(); }
        }

        short _visibility;
        public short Visibility
        {
            get => _visibility;
            set { _visibility = value; NotifyChanged(); }
        }

        string _doneDate;
        public string DoneDate
        {
            get => _doneDate;
            set { _doneDate = value; NotifyChanged(); }
        }

        bool _isUnclass;
        public bool IsUnclass
        {
            get => _isUnclass;
            set { _isUnclass = value; NotifyChanged(); }
        }

        string _duration;
        public string Duration
        {
            get => _duration;
            set { _duration = value; NotifyChanged(); }
        }

        string _prepDuration;
        public string PrepDuration
        {
            get => _prepDuration;
            set { _prepDuration = value; NotifyChanged(); }
        }

        string _refDir;
        public string RefDir
        {
            get => _refDir;
            set { _refDir = value; NotifyChanged(); }
        }

        string _refFile;
        public string RefFile
        {
            get => _refFile;
            set { _refFile = value; NotifyChanged(); }
        }

        public DateVM BoxTime_Date { get; set; }

        public TimeVM BoxTime_Time { get; set; }
        
        public RichTextVM Notes { get; set; }

        public PasswordVM Password { get; set; }

        public RawEmailVM RawEmail { get; set; }

        public RepeatInfoVM Repeats { get; set; }

        public BlockLinkVM Links { get; set; } = new BlockLinkVM();
    }
}
