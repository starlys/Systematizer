using System;
using System.Windows;
using Systematizer.Common;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Systematizer.WPF
{
    /// <summary>
    /// Allow user to export HTML, or export/import CSV
    /// </summary>
    public partial class ExportHtmlDialog : Window
    {
        long? CatId;
        long[] ContextBoxIds, ContextPersonIds;

        public ExportHtmlDialog()
        {
            InitializeComponent();
        }

        public static void ShowExportDialog(long[] contextBoxIds, long[] contextPersonIds)
        {
            var dialog = new ExportHtmlDialog()
            {
                Owner = App.Current.MainWindow,
                ContextBoxIds = contextBoxIds,
                ContextPersonIds = contextPersonIds
            };
            if (contextPersonIds != null)
                dialog.eContextNote.Text = $"This will only export the {contextPersonIds.Length} found record(s) in the active pane.";
            else if (contextBoxIds != null)
                dialog.eContextNote.Text = $"This will only export the {contextBoxIds.Length} found record(s) in the active search pane. (These may be sub-items of a the active note.)";
            else
                dialog.eContextNote.Text = "All records will be exported";

            dialog.ShowDialog();
        }

        string AskForExportFileName(bool isHtml)
        {
            var fileDlg = new SaveFileDialog();
            fileDlg.Filter = isHtml ? "HTML|*.html" : "CSV|*.csv";
            if (fileDlg.ShowDialog(App.Current.MainWindow) != true) return null;
            return fileDlg.FileName;
        }

        string AskForImportFileName(bool isHtml)
        {
            var fileDlg = new OpenFileDialog();
            fileDlg.Filter = isHtml ? "HTML|*.html" : "CSV|*.csv";
            if (fileDlg.ShowDialog(App.Current.MainWindow) != true) return null;
            return fileDlg.FileName;
        }

        /// <summary>
        /// Write all lines to file and inform user
        /// </summary>
        void WriteLinesTo(string writeToPath, IEnumerable<string> lines)
        {
            using (var stream = new StreamWriter(writeToPath))
            {
                foreach (var line in lines)
                    stream.WriteLine(line);
                stream.Flush();
            }
            VisualUtils.ShowMessageDialog("Export completed");
        }

        void SelectCat_Click(object sender, RoutedEventArgs e)
        {
            CatId = CatSelectDialog.SelectCat("Category to export")?.RowId;
        }

        void ExportPeopleCSV_Click(object sender, RoutedEventArgs e)
        {
            var cvt = new CsvConverter();
            var persons = Globals.UI.LoadPersonsForExport(ContextPersonIds);
            var lines = cvt.ToCsv(persons);
            string filename = AskForExportFileName(false);
            if (filename == null) return;
            WriteLinesTo(filename, lines);
        }

        void ImportPeopleCSV_Click(object sender, RoutedEventArgs e)
        {
            //category to auto-apply
            long? autocCatId = CatSelectDialog.SelectCat("Choose category to apply to each imported person, or cancel to skip")?.RowId;

            string filename = AskForImportFileName(false);
            if (filename == null) return;
            var cvt = new CsvConverter();
            try
            {
                using (var rdr = new StreamReader(filename))
                {
                    var persons = cvt.PersonFromCsv(rdr).ToArray();
                    foreach (var person in persons)
                    {
                        var eperson = new ExtPerson(person, null, null);
                        if (autocCatId != null)
                            eperson.SelectedCatIds = new long[] { autocCatId.Value };
                        Globals.UI.SavePerson(eperson);
                    }
                    VisualUtils.ShowMessageDialog($"Imported {persons.Length} record(s)");
                }
            }
            catch (Exception ex)
            {
                VisualUtils.ShowMessageDialog("Importing failed: " + ex.Message);
            }
        }

        void ExportBoxesCSV_Click(object sender, RoutedEventArgs e)
        {
            var cvt = new CsvConverter();
            var boxes = Globals.UI.LoadBoxesForExport(ContextBoxIds);
            var lines = cvt.ToCsv(boxes);
            string filename = AskForExportFileName(false);
            if (filename == null) return;
            WriteLinesTo(filename, lines);
        }

        void ImportBoxesCSV_Click(object sender, RoutedEventArgs e)
        {
            string filename = AskForImportFileName(false);
            if (filename == null) return;
            var cvt = new CsvConverter();
            try
            {
                using (var rdr = new StreamReader(filename))
                {
                    var boxes = cvt.BoxFromCsv(rdr).ToArray();
                    foreach (var box in boxes)
                        Globals.UI.SaveBox(new ExtBox(box, null), false);
                    VisualUtils.ShowMessageDialog($"Imported {boxes.Length} record(s)");
                }
            }
            catch (Exception ex)
            {
                VisualUtils.ShowMessageDialog("Importing failed: " + ex.Message);
            }
            UIGlobals.Do.RebuildViews(BoxEditingPool.CreateUniversalChangeItem());
        }

        void ExportHtml_Click(object sender, RoutedEventArgs e)
        {
            //what to export
            bool inclAllPersons = eIncludeAllPeople.IsChecked == true,
                inclCatPersons = eIncludeCatPeople.IsChecked == true,
                inclTasks = eIncludeSchedule.IsChecked == true,
                inclNotes = eIncludeNotes.IsChecked == true,
                inclPasswords = eIncludePasswords.IsChecked == true;

            //abort if not rational
            string message = null;
            if (inclCatPersons && CatId == null) message = "Select a category before exporting";
            if (inclAllPersons && inclCatPersons) message = "Choose one type of person export, not both";
            if (!inclAllPersons && !inclCatPersons && !inclTasks && !inclNotes) message = "Select something to export";
            if (message != null)
            {
                VisualUtils.ShowMessageDialog(message);
                return;
            }

            //get filename
            string filename = AskForExportFileName(true);
            if (filename == null) return;

            //export
            HtmlExporter.ExportHtml(filename, inclAllPersons, inclCatPersons ? CatId : null, inclTasks, inclNotes, inclPasswords);
            VisualUtils.ShowMessageDialog("Export complete");
        }
    }
}
