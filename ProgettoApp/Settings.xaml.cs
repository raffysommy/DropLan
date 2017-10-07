using Ookii.Dialogs.Wpf;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace ProgettoApp
{
    /// <summary>
    /// Logica di interazione per Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            SaveInDefault.IsChecked = Properties.Settings.Default.IsStorageSet;
            AcceptAll.IsChecked = Properties.Settings.Default.IsAutoAccept;
            SelectedFolder.Text=Properties.Settings.Default.StorageDir;
            AcceptAll.Checked += AcceptAll_Change;
            AcceptAll.Unchecked += AcceptAll_Change;
            SaveInDefault.Checked += SaveInDefault_Change;
            SaveInDefault.Unchecked += SaveInDefault_Change;
            SelectedFolder.LostFocus += SelectedFolder_LostFocus;
        }
        /// <summary>
        /// Finish the edit of the path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedFolder_LostFocus(object sender, RoutedEventArgs e)
        {
            SavePath();
        }
        /// <summary>
        /// Auto save settings on close
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e) {
            SavePath(false);
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        private void SaveInDefault_Change(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.IsStorageSet = SaveInDefault.IsChecked;
        }

        private void AcceptAll_Change(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.IsAutoAccept = AcceptAll.IsChecked;

        }
        /// <summary>
        /// Show folder selection dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderSelect_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderDiag = new VistaFolderBrowserDialog();
            if (folderDiag.ShowDialog().Value)
            {
                SelectedFolder.Text = folderDiag.SelectedPath;
                //A dirty hack
                SelectedFolder.ScrollToHorizontalOffset(SelectedFolder.Text.Length * 5);
                SavePath();
            }
        }
        /// <summary>
        /// Check authorization on selected folder and revert in case of error.
        /// </summary>
        /// <param name="showErr"></param>
        private void SavePath(bool showErr = true) {
            if (SaveInDefault.IsChecked)
            {
                try
                {
                    System.Security.AccessControl.DirectorySecurity acs = Directory.GetAccessControl(SelectedFolder.Text);
                    Properties.Settings.Default.StorageDir = SelectedFolder.Text;
                }
                catch
                {
                    if (showErr)
                    {
                        MessageBox.Show("Il Path inserito è invalido o non disponi di sufficienti diritti di accesso. Scegli un altra cartella!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Properties.Settings.Default.IsStorageSet = false;
                    Properties.Settings.Default.StorageDir = "";
                }
            }
        }
    }
}
