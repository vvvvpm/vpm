using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Path = System.IO.Path;

namespace vpm
{
    /// <summary>
    /// Interaction logic for ChooseDir.xaml
    /// </summary>
    public partial class ChooseDir
    {
        public string PathFromVpm { get; set; }
        public string PathResult { get; set; }
        public bool Cancelled { get; set; }

        public ChooseDir(string pathin)
        {
            PathFromVpm = pathin;
            InitializeComponent();
        }

        private void ChooseDir_OnInitialized_OnInitialized(object sender, EventArgs e)
        {
            DirBox.Text = PathFromVpm;
        }

        private void OnDirDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null)
                {
                    DirBox.Text = files[0];
                }
            }
        }

        private void OnBrowseDir(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false
            };
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                DirBox.Text = dialog.FileName;
            }
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            Cancelled = false;
            PathResult = DirBox.Text;
            VpmConfig.Instance.WaitSignal = false;
            VpmConfig.Instance.DirWindow.Close();
        }
        
        private void OnCancelled(object sender, EventArgs e)
        {
            Cancelled = true;
            VpmConfig.Instance.WaitSignal = false;
            VpmConfig.Instance.DirWindow.Close();
        }
    }
}
