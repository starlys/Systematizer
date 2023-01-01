namespace Systematizer.WPF;

/// <summary>
/// Allow multi-select categories 
/// </summary>
public partial class CatMultiselectDialog : Window
{
    class VM : BaseVM
    {
        public class Item : BaseVM
        {
            public readonly VM Owner;
            public long RowId;
            public bool IsSelected { get; set; }
            public bool IsExpanded { get; set; }
            public string Name { get; set; }
            public List<Item> Children { get; set; }

            public Item(VM owner)
            {
                Owner = owner;
            }

            public Visibility CheckboxVisibility => ToVisibility(Owner.ShowNonLeafCheckboxes || Children == null);
        }

        public bool ShowNonLeafCheckboxes;

        public RangeObservableCollection<Item> Roots { get; set; } = new RangeObservableCollection<Item>();

        public VM(long[] catIds)
        {
            var roots = ToItemList(Globals.AllCats.Roots, catIds) ?? new List<Item>();
            Roots.Clear();
            Roots.AddRange(roots);
        }

        /// <summary>
        /// return the list of currently selected cats (including
        /// those that were selected before)
        /// </summary>
        public long[] GetEditedSelectedIds()
        {
            var selectedIds = new List<long>();
            foreach (var c in Roots)
                AddSelection(selectedIds, c);
            return selectedIds.ToArray();
        }

        Item ToItem(CatCache.Item it, long[] selectedCatIds) => new(this)
        {
            RowId = it.RowId,
            IsExpanded = true,
            IsSelected = selectedCatIds.Contains(it.RowId),
            Name = it.Name,
            Children = ToItemList(it.Children, selectedCatIds)
        };

        List<Item> ToItemList(List<CatCache.Item> children, long[] selectedCatIds)
        {
            if (children == null || children.Count == 0) return null;
            return children.Select(c => ToItem(c, selectedCatIds)).ToList();
        }

        /// <summary>
        /// build up list of currently selecteditems, recursively; return true if this or any children were dirty
        /// </summary>
        bool AddSelection(List<long> selectedIds, Item node)
        {
            bool anyDirty = node.IsDirty;
            if (node.IsSelected) selectedIds.Add(node.RowId);
            if (node.Children != null)
                foreach (var c in node.Children)
                    anyDirty |= AddSelection(selectedIds, c);
            return anyDirty;
        }
    }

    VM _VM;

    public CatMultiselectDialog()
    {
        InitializeComponent();
    }

    VM.Item SelectedItem => eTree.SelectedItem as VM.Item;

    /// <summary>
    /// Allow modifying the categories in the given ExtPerson; returns true if possibly changed
    /// </summary>
    public static bool SelectCats(ExtPerson ep)
    {
        var dialog = new CatMultiselectDialog
        {
            Owner = Application.Current.MainWindow,
            _VM = new VM(ep.SelectedCatIds)
        };
        dialog.DataContext = dialog._VM;
        dialog.eCaption.Text = $"Choose categories for {ep.Person.Name}";
        if (dialog.ShowDialog() != true) return false;
        ep.SelectedCatIds = dialog._VM.GetEditedSelectedIds();
        return true;
    }

    /// <summary>
    /// Allow selecting categories (such as for searching); returns null on cancel
    /// </summary>
    public static long[] SelectCats(bool allowNonLeafSelection, string caption)
    {
        var dialog = new CatMultiselectDialog
        {
            Owner = Application.Current.MainWindow,
            _VM = new VM(Array.Empty<long>())
            {
                ShowNonLeafCheckboxes = allowNonLeafSelection
            }
        };
        dialog.DataContext = dialog._VM;
        dialog.eCaption.Text = caption;
        if (dialog.ShowDialog() != true) return null;
        return dialog._VM.GetEditedSelectedIds();
    }

    void AddCat_Click(object sender, RoutedEventArgs e)
    {
        var prevSelectedCatIds = _VM.GetEditedSelectedIds();

        //get whether root or child
        long? parentId = null;
        var sel = SelectedItem;
        if (sel != null)
        {
            var answer = MessageBox.Show($"Add as sub-category of {sel.Name}? (If No, it will be created at the root level.)", "New Category", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes) parentId = sel.RowId;
            else if (answer != MessageBoxResult.No) return;
        }

        //get name
        var name = StringDialog.GetInput("Category name", "", 40);
        if (name == null) return;
        name = name.Trim();
        if (name.Length == 0) return;

        //save
        UIService.ModifyCat(0, c =>
        {
            c.Name = name;
            c.ParentId = parentId;
        });

        //reload
        _VM = new VM(prevSelectedCatIds);
        DataContext = _VM;
    }

    void Done_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
