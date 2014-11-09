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
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace YTub.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        private string _dirpath;

        private string _mpcpath;

        private string _result;

        private bool _isSyncOnStart;

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

        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                OnPropertyChanged("Result");
            }
        }

        public string MpcPath
        {
            get { return _mpcpath; }
            set
            {
                _mpcpath = value;
                OnPropertyChanged("MpcPath");
            }
        }

        public bool IsSyncOnStart
        {
            get { return _isSyncOnStart; }
            set
            {
                _isSyncOnStart = value;
                OnPropertyChanged("IsSyncOnStart");
            }
        }

        public SettingsModel(string savepath, string mpcpath, int synconstart)
        {
            DirPath = savepath;
            MpcPath = mpcpath;
            IsSyncOnStart = synconstart == 1;
            SaveCommand = new RelayCommand(SaveSettings);
            OpenDirCommand = new RelayCommand(OpenDir);
        }

        private void SaveSettings(object obj)
        {
            var ressync = IsSyncOnStart ? 1 : 0;
            Sqllite.UpdateSetting(Subscribe.ChanelDb, "synconstart", ressync);

            var savedir = new DirectoryInfo(DirPath);
            if (savedir.Exists)
            {
                Sqllite.UpdateSetting(Subscribe.ChanelDb, "savepath", savedir.FullName);
                Subscribe.DownloadPath = savedir.FullName;
                Result = "Saved";
            }
            else
            {
                Result = "Not saved, check Save dir";
            }

            if (!string.IsNullOrEmpty(MpcPath))
            {
                var fnmpc = new FileInfo(MpcPath);
                if (fnmpc.Exists)
                {
                    Sqllite.UpdateSetting(Subscribe.ChanelDb, "pathtompc", fnmpc.FullName);
                    Subscribe.MpcPath = fnmpc.FullName;
                    Result = "Saved";
                }
                else
                {
                    Result = "Not saved, check MPC exe path";
                }
            }
        }

        private void OpenDir(object obj)
        {
            switch (obj.ToString())
            {
                case "DirPath":
                    var dlg = new FolderBrowserDialog();
                    var res = dlg.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        DirPath = dlg.SelectedPath;
                    }
                    break;

                case "MpcPath":
                    var dlgf = new OpenFileDialog();
                    var resf = dlgf.ShowDialog();
                    if (resf == DialogResult.OK)
                    {
                        MpcPath = dlgf.FileName;
                    }
                    break;
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
