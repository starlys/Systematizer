using System;
using System.Linq;
using System.Windows;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class ExtPersonVM : BaseEditableBlockVM
    {
        public ExtPerson Persistent { get; private set; }

        //injected behaviors
        public Action<BlockLinkVM.ItemVM> LinkClicked;

        public ExtPersonVM(ExtPerson person, Action<BaseBlockVM> gotFocusAction) : base(gotFocusAction)
        {
            Persistent = person;
            InitializeFromPersistent();
        }

        public override string BlockTitle => "Person";

        protected override void EditModeChanged()
        {
            Notes.IsEditMode = IsEditMode;
        }

        public override bool IsDirty
        {
            get => base.IsDirty; 
        }

        public override void InitializeFromPersistent()
        {
            Name = Persistent.Person.Name;
            MainPhone = Persistent.Person.MainPhone;
            MainEmail = Persistent.Person.MainEmail;
            Address = Persistent.Person.Address;
            Notes = new RichTextVM { Text = Persistent.Person.Notes };
            Custom1 = Persistent.Person.Custom1;
            Custom2 = Persistent.Person.Custom2;
            Custom3 = Persistent.Person.Custom3;
            Custom4 = Persistent.Person.Custom4;
            Custom5 = Persistent.Person.Custom5;
            InitializeLinksFromPersistent();
            InitializeCatsFromPersistent();
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

        public void InitializeCatsFromPersistent()
        {
            CatNames.Clear();
            foreach (long catId in Persistent.SelectedCatIds)
            {
                string s = Globals.AllCats.GetReadableLineage(catId, true);
                if (s != null)
                    CatNames.Add(s);
            }
        }

        public override void WriteToPersistent()
        {
            Persistent.Person.Name = Name;
            Persistent.Person.MainPhone = MainPhone;
            Persistent.Person.MainEmail = MainEmail;
            Persistent.Person.Address = Address;
            Persistent.Person.Notes = Notes.Text;
            Persistent.Person.Custom1 = Custom1;
            Persistent.Person.Custom2 = Custom2;
            Persistent.Person.Custom3 = Custom3;
            Persistent.Person.Custom4 = Custom4;
            Persistent.Person.Custom5 = Custom5;
        }

        string _name;
        public string Name
        {
            get => _name;
            set { _name = value; NotifyChanged(); }
        }

        string _mainPhone;
        public string MainPhone
        {
            get => _mainPhone;
            set { _mainPhone = value; NotifyChanged(); }
        }

        string _mainEmail;
        public string MainEmail
        {
            get => _mainEmail;
            set { _mainEmail = value; NotifyChanged(); }
        }

        string _address;
        public string Address
        {
            get => _address;
            set { _address = value; NotifyChanged(); }
        }

        string _custom1;
        public string Custom1
        {
            get => _custom1;
            set { _custom1 = value; NotifyChanged(); }
        }
        string _custom2;
        public string Custom2
        {
            get => _custom2;
            set { _custom2 = value; NotifyChanged(); }
        }

        string _custom3;
        public string Custom3
        {
            get => _custom3;
            set { _custom3 = value; NotifyChanged(); }
        }

        string _custom4;
        public string Custom4
        {
            get => _custom4;
            set { _custom4 = value; NotifyChanged(); }
        }

        string _custom5;
        public string Custom5
        {
            get => _custom5;
            set { _custom5 = value; NotifyChanged(); }
        }

        //controller needs to ensure custom label1-5 are null when not used (not blank)
        public string CustomLabel1 { get; set; }
        public string CustomLabel2 { get; set; }
        public string CustomLabel3 { get; set; }
        public string CustomLabel4 { get; set; }
        public string CustomLabel5 { get; set; }

        public Visibility Custom1Visibility => ToVisibility(CustomLabel1 != null);
        public Visibility Custom2Visibility => ToVisibility(CustomLabel2 != null);
        public Visibility Custom3Visibility => ToVisibility(CustomLabel3 != null);
        public Visibility Custom4Visibility => ToVisibility(CustomLabel4 != null);
        public Visibility Custom5Visibility => ToVisibility(CustomLabel5 != null);

        public RichTextVM Notes { get; set; }

        public BlockLinkVM Links { get; set; } = new BlockLinkVM();

        public RangeObservableCollection<string> CatNames { get; set; } = new RangeObservableCollection<string>();
    }
}
