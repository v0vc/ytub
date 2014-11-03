using System;
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

        private Chanel _currentChanel;

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

        public Subscribe()
        {
            var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (dir == null) return;
            ChanelDb = Path.Combine(dir, "ytub.db");
            ChanelList = new ObservableCollection<Chanel>();
            _bgv.WorkerReportsProgress = true;
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument == null) return;
            switch (e.Argument.ToString())
            {
                case "SyncChanel":
                    SyncChanelBgv();
                    break;
            }
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
            if (CurrentChanel == null)
                return;
            Sqllite.RemoveChanelFromDb(ChanelDb, CurrentChanel.ChanelOwner);
            ChanelList.Remove(CurrentChanel);
            if (ChanelList.Any())
                CurrentChanel = ChanelList[0];
        }

        public void SyncChanel(object obj)
        {
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync("SyncChanel");
        }

        public void GetChanelsFromDb()
        {
            var fn = new FileInfo(ChanelDb);
            if (!fn.Exists) return;
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
        }

        private void SyncChanelBgv()
        {
            if (CurrentChanel == null)
                return;
            Application.Current.Dispatcher.Invoke(() => CurrentChanel.ListVideoItems.Clear());
            
            CurrentChanel.GetChanelVideoItems(CurrentChanel.MinRes);

            var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (dir != null)
            {
                int totalrow;
                Sqllite.CreateOrConnectDb(ChanelDb, CurrentChanel.ChanelOwner, out totalrow);
                if (totalrow == 0)
                {
                    foreach (VideoItem videoItem in CurrentChanel.ListVideoItems)
                    {
                        Sqllite.InsertRecord(ChanelDb, videoItem.VideoID, CurrentChanel.ChanelOwner,
                            CurrentChanel.ChanelName, videoItem.VideoLink, videoItem.Title, videoItem.ViewCount,
                            videoItem.Duration, videoItem.Published, videoItem.Description);
                    }
                }
                else
                {
                    foreach (VideoItem videoItem in CurrentChanel.ListVideoItems)
                    {
                        VideoItem item = videoItem;
                        Application.Current.Dispatcher.Invoke(() => item.IsSynced = Sqllite.IsTableHasRecord(ChanelDb, item.VideoID));
                    }
                    CurrentChanel.IsReady = !CurrentChanel.ListVideoItems.Select(x => x.IsSynced).Contains(false);

                    foreach (VideoItem videoItem in CurrentChanel.ListVideoItems.Where(x => x.IsSynced == false))
                    {
                        Sqllite.InsertRecord(ChanelDb, videoItem.VideoID, CurrentChanel.ChanelOwner,
                            CurrentChanel.ChanelName, videoItem.VideoLink, videoItem.Title, videoItem.ViewCount,
                            videoItem.Duration, videoItem.Published, videoItem.Description);
                    }
                }
            }
        }
    }
}
