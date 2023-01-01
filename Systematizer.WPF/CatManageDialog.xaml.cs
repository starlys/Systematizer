namespace Systematizer.WPF;

/// <summary>
/// Interaction logic for CatManageDialog.xaml
/// </summary>
public partial class CatManageDialog : Window
{
    CatCache.Item SelectedCat => eTree.SelectedItem as CatCache.Item;

    class VM : BaseVM
    {
        public VM()
        {
            Roots = Globals.AllCats.Roots;
        }

        public List<CatCache.Item> Roots { get; set; }
    }

    public CatManageDialog()
    {
        InitializeComponent();
    }

    public static void ManageCats()
    {
        var dialog = new CatManageDialog
        {
            Owner = App.Current.MainWindow,
            DataContext = new VM()
        };
        dialog.ShowDialog();
    }

    void Add_Click(object sender, RoutedEventArgs e)
    {
        var sel = SelectedCat;
        long? parentId = null;
        if (sel != null)
        {
            var kindOfAdd = MessageBox.Show("Create a sub-item of the selected category? (If No, it will be created at the root level.)", "Add Category", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (kindOfAdd == MessageBoxResult.Yes)
                parentId = sel.RowId;
            else if (kindOfAdd != MessageBoxResult.No)
                return; //canceled
        }
        string s = StringDialog.GetInput("New category name", "", 40);
        if (s == null) return;
        UIService.ModifyCat(0, cat =>
        {
            cat.ParentId = parentId;
            cat.Name = s;
        });
        DataContext = new VM();
    }

    void Rename_Click(object sender, RoutedEventArgs e)
    {
        var sel = SelectedCat;
        if (sel == null) return;
        string s = StringDialog.GetInput("New name", sel.Name, 40);
        if (s == null) return;
        UIService.ModifyCat(sel.RowId, cat =>
        {
            cat.Name = s;
        });
        DataContext = new VM();
    }

    void Move_Click(object sender, RoutedEventArgs e)
    {
        var sel = SelectedCat;
        if (sel == null) return;
        var kindOfMove = MessageBox.Show("Make this a sub-item of another category? (If No, it will be moved to the root level.)", "Move Category", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        long? newParentId = null;
        if (kindOfMove == MessageBoxResult.Yes)
        {
            var item = CatSelectDialog.SelectCat("The category will become a sub-item of the item you select here");
            if (item == null) return;
            newParentId = item.RowId;
        }
        else if (kindOfMove != MessageBoxResult.No)
            return; //canceled
        UIService.ModifyCat(sel.RowId, cat =>
        {
            cat.ParentId = newParentId;
        });
        DataContext = new VM();
    }

    void Delete_Click(object sender, RoutedEventArgs e)
    {
        var sel = SelectedCat;
        if (sel == null) return;
        var warning = UIService.GetCategoryDeleteWarning(sel.RowId);

        //warning or not allowed: abort
        if (warning.Item2 != null)
        {
            if (MessageBox.Show(App.Current.MainWindow, warning.Item2, "Systematizer", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
                return;
        }
        if (!warning.Item1)
            return;

        //delete it
        UIService.DeleteCategory(sel.RowId);
        DataContext = new VM();
    }
}
