using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Systematizer.Common;

namespace Systematizer.WPF
{
    /// <summary>
    /// Controller for SystemDialog
    /// </summary>
    class SystemController
    {
        const string FILEFILTER = "Datbase|*.sqlite";

        SystemDialog SysDlg;

        public void ShowDialog()
        {
            //init dialog
            SysDlg = new SystemDialog
            {
                Owner = App.Current.MainWindow
            };
            foreach (string path in RecentFilesList.GetRecentFiles())
                SysDlg.eFileList.Items.Add(path);
            SysDlg.eOptionsPanel.Visibility = Globals.DatabasePath == null ? Visibility.Collapsed : Visibility.Visible;
            SysDlg.eAllowScheduled.IsChecked = Globals.AllowTasks;
            SysDlg.eCustom1.Text = Globals.PersonCustomLabels?[0];
            SysDlg.eCustom2.Text = Globals.PersonCustomLabels?[1];
            SysDlg.eCustom3.Text = Globals.PersonCustomLabels?[2];
            SysDlg.eCustom4.Text = Globals.PersonCustomLabels?[3];
            SysDlg.eCustom5.Text = Globals.PersonCustomLabels?[4];

            //handle events
            SysDlg.eCreateButton.Click += (s, e) =>
            {
                var saveAs = new SaveFileDialog
                {
                    Filter = FILEFILTER
                };
                if (saveAs.ShowDialog(SysDlg) != true) return;

                if (!CopyTemplateDatabaseTo(saveAs.FileName)) return;
                if (!UIGlobals.Do.OpenDatabaseWithErrorReporting(saveAs.FileName)) return;
                SysDlg.Close();
            };
            void openFromList()
            {
                if (SysDlg.eFileList.SelectedItem is not string sel) return;
                if (!UIGlobals.Do.OpenDatabaseWithErrorReporting(sel)) return;
                SysDlg.Close();
            };
            SysDlg.eFileList.MouseDoubleClick += (s, e) => openFromList();
            SysDlg.eOpenSelectedButton.Click += (s, e) => openFromList();

            SysDlg.eOpenOtherButton.Click += (s, e) =>
            {
                var open = new OpenFileDialog
                {
                    Filter = FILEFILTER
                };
                if (open.ShowDialog(SysDlg) != true) return;

                if (!UIGlobals.Do.OpenDatabaseWithErrorReporting(open.FileName)) return;
                SysDlg.Close();
            };
            SysDlg.eForgetButton.Click += (s, e) =>
            {
                if (SysDlg.eFileList.SelectedItem is not string sel) return;
                SysDlg.eFileList.Items.RemoveAt(SysDlg.eFileList.SelectedIndex);
                RecentFilesList.ForgetPath(sel);
            };
            SysDlg.eAllowScheduled.Checked += (s, e) =>
            {
                Globals.AllowTasks = true;
                DBUtil.WriteSettings(s => s.AllowTasks = 1);
            };
            SysDlg.eAllowScheduled.Unchecked += (s, e) =>
            {
                Globals.AllowTasks = false;
                DBUtil.WriteSettings(s => s.AllowTasks = 0);
            };
            SysDlg.eCustom1.LostFocus += (s, e) => SaveCustomLabel(s, 1);
            SysDlg.eCustom2.LostFocus += (s, e) => SaveCustomLabel(s, 2);
            SysDlg.eCustom3.LostFocus += (s, e) => SaveCustomLabel(s, 3);
            SysDlg.eCustom4.LostFocus += (s, e) => SaveCustomLabel(s, 4);
            SysDlg.eCustom5.LostFocus += (s, e) => SaveCustomLabel(s, 5);
            SysDlg.eDoneButton.Click += (s, e) =>
            {
                SysDlg.Close();
            };

            SysDlg.ShowDialog();
        }

        static void SaveCustomLabel(object sender, int labelNo)
        {
            string oldLabel = Globals.PersonCustomLabels[labelNo - 1];
            string newLabel = ((TextBox)sender).Text;
            if (oldLabel != newLabel)
            {
                Globals.PersonCustomLabels[labelNo - 1] = newLabel;
                DBUtil.WriteSettings(s =>
                {
                    if (labelNo == 1) s.Custom1Label = newLabel;
                    else if (labelNo == 2) s.Custom2Label = newLabel;
                    else if (labelNo == 3) s.Custom3Label = newLabel;
                    else if (labelNo == 4) s.Custom4Label = newLabel;
                    else if (labelNo == 5) s.Custom5Label = newLabel;
                });
            }
        }

        //true on success
        static bool CopyTemplateDatabaseTo(string fileName)
        {
            try
            {
                string fromPath = Path.Combine(Globals.UI.GetExeDirectory(), "template.sqlite");
                File.Copy(fromPath, fileName);
                return true;
            }
            catch(Exception ex)
            {
                VisualUtils.ShowMessageDialog("Error copying template database to the selected location: " + ex.Message);
                return false;
            }
        }
    }
}
