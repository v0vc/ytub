using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using YTub.Common;

namespace YTub.Models
{
    public class SettingsModel :INotifyPropertyChanged
    {
        private string _dirpath;

        public RelayCommand SaveCommand { get; set; }

        public RelayCommand OpenDirCommand { get; set; }

        public string DirPath
        {
            get { return _dirpath; }
            set
            {
                _dirpath = value;
                OnPropertyChanged("DirPath");
            }
        }

        public SettingsModel(string path)
        {
            DirPath = path;
            SaveCommand = new RelayCommand(SaveSettings);
            OpenDirCommand = new RelayCommand(OpenDir);
        }

        private void SaveSettings(object obj)
        {
            var dir = new DirectoryInfo(DirPath);
            if (dir.Exists)
            {
                Sqllite.UpdateDownloadPath(Subscribe.ChanelDb, dir.FullName);
                MessageBox.Show(@"Saved", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(@"Check Directory", @"Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenDir(object obj)
        {
            var dlg = new FolderBrowserDialog();
            var res = dlg.ShowDialog();
            if (res == DialogResult.OK)
            {
                DirPath = dlg.SelectedPath;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
