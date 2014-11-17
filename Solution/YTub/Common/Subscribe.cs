using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SevenZip;
using YTub.Models;
using YTub.Views;

namespace YTub.Common
{
    public class Subscribe : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public static string ChanelDb;

        public static string DownloadPath;

        public static string MpcPath;

        public static string YoudlPath;

        public static string FfmpegPath;

        private string _result;

        private Chanel _currentChanel;

        private IList _selectedListChanels = new ArrayList();

        public Chanel CurrentChanel
        {
            get { return _currentChanel; }
            set
            {
                _currentChanel = value;
                OnPropertyChanged("CurrentChanel");
            }
        }

        public ObservableCollection<Chanel> ChanelList { get; set; }

        public TimeSpan Synctime { get; set; }

        public bool IsSyncOnStart { get; set; }

        public IList SelectedListChanels
        {
            get { return _selectedListChanels; }
            set
            {
                _selectedListChanels = value;
                OnPropertyChanged("SelectedListChanels");
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

        public Subscribe()
        {
            var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (dir == null) return;
            ChanelDb = Path.Combine(dir, "ytub.db");
            ChanelList = new ObservableCollection<Chanel>();
            SevenZipBase.SetLibraryPath(Path.Combine(dir, "7z.dll"));
            var fn = new FileInfo(ChanelDb);
            if (fn.Exists)
            {
                DownloadPath = Sqllite.GetSettingsValue(ChanelDb, "savepath");
                MpcPath = Sqllite.GetSettingsValue(ChanelDb, "pathtompc");
                IsSyncOnStart = Sqllite.GetSettingsIntValue(ChanelDb, "synconstart") != 0;
                YoudlPath = Sqllite.GetSettingsValue(ChanelDb, "pathtoyoudl");
                FfmpegPath = Sqllite.GetSettingsValue(ChanelDb, "pathtoffmpeg");
            }
        }

        public void AddChanel(object o)
        {
            var isEdit = o != null && o.ToString() == "edit";
            try
            {
                var addChanelModel = new AddChanelModel(null, isEdit);
                if (isEdit)
                {
                    addChanelModel.ChanelOwner = CurrentChanel.ChanelOwner;
                    addChanelModel.ChanelName = CurrentChanel.ChanelName;
                }

                var addChanelView = new AddChanelView
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = addChanelModel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                addChanelModel.View = addChanelView;

                if (isEdit)
                {
                    addChanelView.TextBoxLink.IsEnabled = false;
                    addChanelView.TextBoxName.Focus();
                }
                else
                {
                    addChanelView.TextBoxLink.Focus();    
                }

                addChanelView.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RemoveChanel(object obj)
        {
            if (SelectedListChanels.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (Chanel chanel in SelectedListChanels)
                {
                    sb.Append(chanel.ChanelName).Append(Environment.NewLine);
                }
                var result = MessageBox.Show("Are you sure to delete:" + Environment.NewLine + sb + "?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    for (var i = SelectedListChanels.Count; i > 0; i--)
                    {
                        var chanel = SelectedListChanels[i - 1] as Chanel;
                        if (chanel == null) continue;
                        Sqllite.RemoveChanelFromDb(ChanelDb, chanel.ChanelOwner);
                        ChanelList.Remove(chanel);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select Chanell");
            }
            
            if (ChanelList.Any())
                CurrentChanel = ChanelList[0];
        }

        public void SyncChanel(object obj)
        {
            Result = string.Empty;

            switch (obj.ToString())
            {
                case "SyncChanelAll":

                    ChanelSync(ChanelList);

                    break;

                case "SyncChanelSelected":

                    ChanelSync(SelectedListChanels);

                    break;
            }
        }

        public void GetChanelsFromDb()
        {   
            var fn = new FileInfo(ChanelDb);
            if (!fn.Exists)
            {
                Sqllite.CreateDb(ChanelDb);
                return;
            }

            foreach (KeyValuePair<string, string> pair in Sqllite.GetDistinctValues(ChanelDb, "chanelowner", "chanelname"))
            {
                ChanelList.Add(new Chanel(pair.Value, pair.Key));
            }

            foreach (Chanel chanel in ChanelList)
            {
                chanel.GetChanelVideoItemsFromDb(ChanelDb);
            }

            if (ChanelList.Any())
                CurrentChanel = ChanelList[0];

            if (IsSyncOnStart)
                SyncChanel("SyncChanelAll");
        }

        private static void ChanelSync(ICollection list)
        {
            if (list == null || list.Count <= 0) return;

            foreach (Chanel chanel in list)
            {
                chanel.GetChanelVideoItems();
            }
        }

        public void PlayFile(object obj)
        {
            if (CurrentChanel != null && CurrentChanel.CurrentVideoItem != null)
            {
                CurrentChanel.CurrentVideoItem.RunFile(obj);
            }
        }
    }
}
