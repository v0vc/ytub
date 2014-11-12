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

        private Chanel _currentChanel;

        private IList _selectedListChanels = new ArrayList();

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

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
            _bgv.WorkerReportsProgress = true;
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument == null) 
                return;

            SyncChanelBgv(e.Argument.ToString());
        }

        static void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (e.Error is SQLiteException)
                {
                    MessageBox.Show(e.Error.Message, "Database exception", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(e.Error.Message, "Common error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            switch (obj.ToString())
            {
                case "SyncChanelAll":
                    if (!_bgv.IsBusy)
                        _bgv.RunWorkerAsync("SyncChanelAll");
                    break;

                case "SyncChanelSelected":
                    if (!_bgv.IsBusy)
                        _bgv.RunWorkerAsync("SyncChanelSelected");
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

        private void SyncChanelBgv(string wtype)
        {
            switch (wtype)
            {
                case "SyncChanelAll":

                    ChanelSync(ChanelList);

                    break;

                case "SyncChanelSelected":

                    ChanelSync(SelectedListChanels);

                    break;
            }
            //if (ChanelList.Any())
            //    CurrentChanel = ChanelList[0];
        }

        private static void ChanelSync(ICollection list)
        {
            if (list != null && list.Count > 0)
            {
                foreach (Chanel chanel in list)
                {
                    Chanel chanel1 = chanel;
                    chanel1.GetChanelVideoItems(chanel1.MinRes);

                    var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    if (dir != null)
                    {
                        int totalrow;
                        Sqllite.CreateOrConnectDb(ChanelDb, chanel1.ChanelOwner, out totalrow);
                        if (totalrow == 0)
                        {
                            foreach (VideoItem videoItem in chanel1.ListVideoItems)
                            {
                                Sqllite.InsertRecord(ChanelDb, videoItem.VideoID, chanel1.ChanelOwner,
                                    chanel1.ChanelName, videoItem.VideoLink, videoItem.Title, videoItem.ViewCount,
                                    videoItem.Duration, videoItem.Published, videoItem.Description);
                                VideoItem item = videoItem;
                                Application.Current.Dispatcher.Invoke(() => item.IsHasFile = item.IsFileExist(item));
                            }
                        }
                        else
                        {
                            foreach (VideoItem videoItem in chanel1.ListVideoItems)
                            {
                                VideoItem item = videoItem;
                                Application.Current.Dispatcher.Invoke(() => item.IsSynced = Sqllite.IsTableHasRecord(ChanelDb, item.VideoID));
                                Application.Current.Dispatcher.Invoke(() => item.IsHasFile = item.IsFileExist(item));
                            }

                            Application.Current.Dispatcher.Invoke(() => chanel1.IsReady = !chanel1.ListVideoItems.Select(x => x.IsSynced).Contains(false));
                            foreach (VideoItem videoItem in chanel1.ListVideoItems.Where(x => x.IsSynced == false))
                            {
                                Sqllite.InsertRecord(ChanelDb, videoItem.VideoID, chanel1.ChanelOwner,
                                    chanel1.ChanelName, videoItem.VideoLink, videoItem.Title, videoItem.ViewCount,
                                    videoItem.Duration, videoItem.Published, videoItem.Description);
                            }
                        }
                    }
                }
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
