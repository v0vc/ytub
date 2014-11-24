using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SevenZip;
using YTub.Models;
using YTub.Views;

namespace YTub.Common
{
    public class Subscribe : INotifyPropertyChanged
    {
        public static string ChanelDb;

        public static string DownloadPath;

        public static string MpcPath;

        public static string YoudlPath;

        public static string FfmpegPath;

        public static bool IsPathContainFfmpeg;

        private bool _isOnlyFavorites;

        private string _result;

        private Chanel _currentChanel;

        private IList _selectedListChanels = new ArrayList();

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        private readonly List<VideoItem> _filterlist = new List<VideoItem>();

        private Timer _timer;

        private string _titleFilter;

        #region Fields

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

        public ObservableCollection<Chanel> ChanelListToBind { get; set; }

        public List<string> ServerList { get; set; }

        public TimeSpan Synctime { get; set; }

        public bool IsSyncOnStart { get; set; }

        public bool IsPopular { get; set; }

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

        public bool IsOnlyFavorites
        {
            get { return _isOnlyFavorites; }
            set
            {
                _isOnlyFavorites = value;
                OnPropertyChanged("IsOnlyFavorites");
                FilterChanell();
            }
        }

        public TrulyObservableCollection<VideoItem> ListPopularVideoItems { get; set; }

        public string TitleFilter
        {
            get { return _titleFilter; }
            set
            {
                _titleFilter = value;
                OnPropertyChanged("TitleFilter");
                Filter();
            }
        }

        #endregion

        public Subscribe()
        {
            var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (dir == null) return;
            Sqllite.AppDir = dir;
            ChanelDb = Path.Combine(dir, "ytub.db");
            ChanelList = new ObservableCollection<Chanel>();
            ChanelListToBind = new ObservableCollection<Chanel>();
            ListPopularVideoItems = new TrulyObservableCollection<VideoItem>();
            SevenZipBase.SetLibraryPath(Path.Combine(dir, "7z.dll"));
            var fn = new FileInfo(ChanelDb);
            if (fn.Exists)
            {
                DownloadPath = Sqllite.GetSettingsValue(ChanelDb, "savepath");
                MpcPath = Sqllite.GetSettingsValue(ChanelDb, "pathtompc");
                IsSyncOnStart = Sqllite.GetSettingsIntValue(ChanelDb, "synconstart") != 0;
                IsOnlyFavorites = Sqllite.GetSettingsIntValue(ChanelDb, "isonlyfavor") != 0;
                IsPopular = Sqllite.GetSettingsIntValue(ChanelDb, "ispopular") != 0;
                YoudlPath = Sqllite.GetSettingsValue(ChanelDb, "pathtoyoudl");
                FfmpegPath = Sqllite.GetSettingsValue(ChanelDb, "pathtoffmpeg");
            }
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            ServerList = new List<string> { "YouTube", "RuTracker" };
        }

        private void Filter()
        {
            if (string.IsNullOrEmpty(TitleFilter))
            {
                if (_filterlist.Any())
                {
                    ListPopularVideoItems.Clear();
                    foreach (VideoItem item in _filterlist)
                    {
                        if (item.Title.Contains(TitleFilter))
                            ListPopularVideoItems.Add(item);
                    }
                }
            }
            else
            {
                if (!_filterlist.Any())
                    _filterlist.AddRange(ListPopularVideoItems);
                ListPopularVideoItems.Clear();
                foreach (VideoItem item in _filterlist)
                {
                    if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                        ListPopularVideoItems.Add(item);
                }
            }
        }

        void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _timer.Dispose();
            Result = string.Format("{0} synced in {1}", ViewModelLocator.MvViewModel.Model.SelectedCountry.Key, Synctime.Duration().ToString(@"mm\:ss"));
        }

        void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            var cul = e.Argument.ToString();
            if (string.IsNullOrEmpty(cul))
                cul = "RU";
            var wc = new WebClient { Encoding = Encoding.UTF8 };
            var zap = string.Format("https://gdata.youtube.com/feeds/api/standardfeeds/{0}/most_popular?time=today&v=2&alt=json", cul);
            string s = wc.DownloadString(zap);
            var jsvideo = (JObject)JsonConvert.DeserializeObject(s);
            if (jsvideo == null)
                return;
            foreach (JToken pair in jsvideo["feed"]["entry"])
            {
                var v = new VideoItem(pair, true, cul + " now") {Num = ListPopularVideoItems.Count + 1};
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ListPopularVideoItems.Add(v);
                    v.IsHasFile = v.IsFileExist(v);
                    v.IsSynced = true;
                });
            }

            zap = string.Format("https://gdata.youtube.com/feeds/api/standardfeeds/{0}/most_popular?time=all_time&v=2&alt=json", cul);
            s = wc.DownloadString(zap);
            jsvideo = (JObject)JsonConvert.DeserializeObject(s);
            if (jsvideo == null)
                return;
            foreach (JToken pair in jsvideo["feed"]["entry"])
            {
                var v = new VideoItem(pair, true, cul + " all") {Num = ListPopularVideoItems.Count + 1};
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ListPopularVideoItems.Add(v);
                    v.IsHasFile = v.IsFileExist(v);
                    v.IsSynced = true;
                });
            }
        }

        public void AddChanel(object o)
        {
            var isEdit = o != null && o.ToString() == "edit";
            try
            {
                var addChanelModel = new AddChanelModel(null, isEdit, ServerList);
                if (isEdit)
                {
                    addChanelModel.ChanelOwner = CurrentChanel.ChanelOwner;
                    addChanelModel.ChanelName = CurrentChanel.ChanelName;
                    addChanelModel.ServerName = CurrentChanel.ServerName;
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
                    addChanelView.TextBoxLink.IsReadOnly = true;
                    addChanelView.TextBoxName.Focus();
                    addChanelView.ComboBoxServers.IsEnabled = false;
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
                        FilterChanell();
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

                case "SyncChanelFavorites":
                    ChanelSync(ChanelList.Where(x => x.IsFavorite).ToList());
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
                ChanelList.Add(new Chanel(pair.Value.Split(':')[0], pair.Key, pair.Value.Split(':')[1]));
            }

            foreach (Chanel chanel in ChanelList)
            {
                chanel.GetChanelVideoItemsFromDb(ChanelDb);
            }

            if (ChanelList.Any())
                CurrentChanel = ChanelList[0];

            IsOnlyFavorites = Sqllite.GetSettingsIntValue(ChanelDb, "isonlyfavor") == 1;

            if (IsSyncOnStart)
            {
                if (IsOnlyFavorites)
                    SyncChanel("SyncChanelFavorites");
                else
                    SyncChanel("SyncChanelAll");
            }

            if (IsPopular)
            {
                var culture = Sqllite.GetSettingsValue(ChanelDb, "culture");
                ViewModelLocator.MvViewModel.Model.SelectedCountry = ViewModelLocator.MvViewModel.Model.Countries.First(x => x.Value == culture);
            }
        }

        public void GetPopularVideos(string culture)
        {
            Synctime =new TimeSpan();
            var tcb = new TimerCallback(tmr_Tick);
            _timer = new Timer(tcb, null, 0, 1000);
            ListPopularVideoItems.Clear();
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync(culture);
        }

        public static void CheckFfmpegPath()
        {
            if (string.IsNullOrEmpty(FfmpegPath))
                IsPathContainFfmpeg = false;
            else
            {
                var fn = new FileInfo(FfmpegPath);
                if (fn.Exists && fn.DirectoryName != null)
                {
                    var winpath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
                    if (winpath != null && winpath.Contains(fn.DirectoryName))
                        IsPathContainFfmpeg = true;
                    else
                        IsPathContainFfmpeg = false;
                }
                else
                    IsPathContainFfmpeg = false;
            }
        }

        private static void ChanelSync(ICollection list)
        {
            if (list == null || list.Count <= 0) return;

            foreach (Chanel chanel in list)
            {
                chanel.GetChanelVideoItems();
            }
        }

        private void FilterChanell()
        {
            ChanelListToBind.Clear();
            if (IsOnlyFavorites)
            {
                foreach (Chanel chanel in ChanelList.Where(x=>x.IsFavorite))
                {
                    ChanelListToBind.Add(chanel);
                }
            }
            else
            {
                foreach (Chanel chanel in ChanelList)
                {
                    ChanelListToBind.Add(chanel);
                }
            }
            if (ChanelListToBind.Any())
                CurrentChanel = ChanelListToBind[0];
        }

        void tmr_Tick(object o)
        {
            Synctime = Synctime.Add(TimeSpan.FromSeconds(1));
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
