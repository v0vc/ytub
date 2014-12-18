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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SevenZip;
using YTub.Chanell;
using YTub.Models;
using YTub.Video;
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

        #region Static

        public static string ChanelDb;

        public static string DownloadPath;

        public static string MpcPath;

        public static string YoudlPath;

        public static string FfmpegPath;

        public static string RtLogin;

        public static string RtPass;

        public static string TapLogin;

        public static string TapPass;

        public static bool IsAsyncDl;

        public static bool IsPopular;

        public static bool IsSyncOnStart;

        #endregion

        #region Fields

        private MainWindowModel _model;

        private bool _isOnlyFavorites;

        private string _result;

        private const string Dbfile = "ytub.db";

        private ChanelBase _currentChanel;

        private IList _selectedListChanels = new ArrayList();

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        private readonly List<VideoItemBase> _filterlist = new List<VideoItemBase>();

        private Timer _timer;

        private string _titleFilter;

        private string _searchKey;

        private int _selectedTabIndex;

        #endregion

        #region Properties

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged("SelectedTabIndex");
            }
        }
        public ChanelBase CurrentChanel
        {
            get { return _currentChanel; }
            set
            {
                _currentChanel = value;
                OnPropertyChanged("CurrentChanel");
                SelectedTabIndex = 0;
            }
        }

        public ChanelBase SelectedForumItem { get; set; }

        public ObservableCollection<ChanelBase> ChanelList { get; set; }

        public ObservableCollection<ChanelBase> ChanelListToBind { get; set; }

        public ObservableCollection<ChanelBase> ServerList { get; set; }

        public TimeSpan Synctime { get; set; }

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
                if (ChanelList.Any())
                    FilterChanell();
            }
        }

        public TrulyObservableCollection<VideoItemBase> ListPopularVideoItems { get; set; }

        public TrulyObservableCollection<VideoItemBase> ListSearchVideoItems { get; set; }

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

        public string SearchKey
        {
            get { return _searchKey; }
            set
            {
                _searchKey = value;
                OnPropertyChanged("SearchKey");
            }
        }

        #endregion

        #region Construction

        public Subscribe(MainWindowModel model)
        {
            _model = model;
            var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (dir == null) return;
            Sqllite.AppDir = dir;
            ChanelDb = Path.Combine(dir, Dbfile);
            ChanelList = new ObservableCollection<ChanelBase>();
            ChanelListToBind = new ObservableCollection<ChanelBase>();
            ListPopularVideoItems = new TrulyObservableCollection<VideoItemBase>();
            ListSearchVideoItems = new TrulyObservableCollection<VideoItemBase>();
            SevenZipBase.SetLibraryPath(Path.Combine(dir, "7z.dll"));
            var fn = new FileInfo(ChanelDb);
            if (fn.Exists)
            {
                Result = "Working...";
                RtLogin = Sqllite.GetSettingsValue(fn.FullName, "rtlogin");
                RtPass = Sqllite.GetSettingsValue(fn.FullName, "rtpassword");
                TapLogin = Sqllite.GetSettingsValue(fn.FullName, "taplogin");
                TapPass = Sqllite.GetSettingsValue(fn.FullName, "tappassword");
                DownloadPath = Sqllite.GetSettingsValue(ChanelDb, "savepath");
                MpcPath = Sqllite.GetSettingsValue(ChanelDb, "pathtompc");
                IsSyncOnStart = Sqllite.GetSettingsIntValue(ChanelDb, "synconstart") != 0;
                IsAsyncDl = Sqllite.GetSettingsIntValue(ChanelDb, "asyncdl") != 0;
                IsOnlyFavorites = Sqllite.GetSettingsIntValue(ChanelDb, "isonlyfavor") != 0;
                IsPopular = Sqllite.GetSettingsIntValue(ChanelDb, "ispopular") != 0;
                YoudlPath = Sqllite.GetSettingsValue(ChanelDb, "pathtoyoudl");
                FfmpegPath = Sqllite.GetSettingsValue(ChanelDb, "pathtoffmpeg");
                ServerList = new ObservableCollection<ChanelBase>
                {
                    new ChanelYou("YouTube", string.Empty, string.Empty, "YouTube", string.Empty, 0),
                    new ChanelRt("RuTracker", RtLogin, RtPass, "RuTracker", string.Empty, 0),
                    new ChanelTap("Tapochek", TapLogin, TapPass, "Tapochek", string.Empty, 0)
                };
            }
            else
            {
                Result = "Ready";
                ServerList = new ObservableCollection<ChanelBase>
                {
                    new ChanelYou("YouTube", string.Empty, string.Empty, "YouTube", string.Empty, 0),
                    new ChanelRt("RuTracker", string.Empty, string.Empty, "RuTracker", string.Empty, 0),
                    new ChanelTap("Tapochek", string.Empty, string.Empty, "Tapochek", string.Empty, 0)
                };
                DownloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            SelectedForumItem = ServerList[0];
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        #endregion

        #region Public Methods

        public void Search(object obj)
        {
            if (string.IsNullOrEmpty(SearchKey))
                return;
            InitializeTimer();
            ListSearchVideoItems.Clear();
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync("Search");
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
                    addChanelModel.SelectedForumItem = ServerList.First(z => z.ChanelType == CurrentChanel.ChanelType);
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
                foreach (ChanelBase chanel in SelectedListChanels)
                {
                    sb.Append(chanel.ChanelName).Append(Environment.NewLine);
                }
                var result = MessageBox.Show("Are you sure to delete:" + Environment.NewLine + sb + "?", "Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    for (var i = SelectedListChanels.Count; i > 0; i--)
                    {
                        var chanel = SelectedListChanels[i - 1] as ChanelBase;
                        if (chanel == null) continue;
                        Sqllite.RemoveChanelFromDb(ChanelDb, chanel.ChanelOwner);
                        ChanelList.Remove(chanel);
                        FilterChanell();
                    }
                    Result = "Deleted";
                }
            }
            else
            {
                MessageBox.Show("Please select Chanell");
            }

            if (ChanelList.Any())
                CurrentChanel = ChanelList[0];
        }

        public void GetChanelsFromDb()
        {
            InitializeTimer();

            var fn = new FileInfo(ChanelDb);
            if (!fn.Exists)
            {
                Sqllite.CreateDb(ChanelDb);
                return;
            }

            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync("GetDB");
        }

        public void GetPopularVideos(string culture)
        {
            InitializeTimer();
            ListPopularVideoItems.Clear();
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync(culture);
        }

        public void SyncChanel(object obj)
        {
            Result = string.Empty;

            switch (obj.ToString())
            {
                case "SyncChanelAll":

                    ChanelSync(ChanelList, false);

                    break;

                case "SyncChanelSelected":

                    ChanelSync(SelectedListChanels, false);

                    break;

                case "SyncAllChanelSelected":

                    ChanelSync(SelectedListChanels, true);

                    break;

                case "SyncChanelFavorites":
                    ChanelSync(ChanelList.Where(x => x.IsFavorite).ToList(), false);
                    break;
            }
        }

        public static void SetResult(string result)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.Result = result;
        }

        #endregion

        #region Private Methods

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            var cul = e.Argument.ToString();
            if (string.IsNullOrEmpty(cul))
                cul = "RU";
            e.Result = cul;

            WebClient wc;
            string zap;
            string res;
            JObject jsvideo;

            switch (cul)
            {
                case "GetDB":

                    #region GetDB

                    foreach (
                        KeyValuePair<string, string> pair in
                            Sqllite.GetDistinctValues(ChanelDb, "chanelowner", "chanelname"))
                    {
                        var sp = pair.Value.Split(':');

                        ChanelBase chanel = null;
                        if (sp[1] == "YouTube")
                            chanel = new ChanelYou(sp[1], "TODO", "TODO", sp[0], pair.Key, Convert.ToInt32(sp[2]));
                        if (sp[1] == "RuTracker")
                            chanel = new ChanelRt(sp[1], RtLogin, RtPass, sp[0], pair.Key, Convert.ToInt32(sp[2]));
                        if (sp[1] == "Tapochek")
                            chanel = new ChanelTap(sp[1], TapLogin, TapPass, sp[0], pair.Key, Convert.ToInt32(sp[2]));

                        ChanelList.Add(chanel);
                    }

                    foreach (ChanelBase chanel in ChanelList)
                    {
                        chanel.GetItemsFromDb();
                    }

                    if (ChanelList.Any())
                        CurrentChanel = ChanelList[0];

                    #endregion

                    break;

                case "Search":

                    switch (SelectedForumItem.ChanelType)
                    {
                        case "YouTube":

                            wc = new WebClient { Encoding = Encoding.UTF8 };
                            zap = string.Format("https://gdata.youtube.com/feeds/api/videos?q={0}&max-results=50&v=2&alt=json", SearchKey);
                            res = wc.DownloadString(zap);
                            jsvideo = (JObject)JsonConvert.DeserializeObject(res);
                            if (jsvideo == null)
                                return;

                            foreach (JToken pair in jsvideo["feed"]["entry"])
                            {
                                var v = new VideoItemYou(pair, true)
                                {
                                    Num = ListSearchVideoItems.Count + 1
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ListSearchVideoItems.Add(v);
                                    v.IsHasFile = v.IsFileExist();
                                    v.IsSynced = true;
                                });
                            }
                            break;

                        case "RuTracker":
                            break;

                        case "Tapochek":
                            break;
                    }

                    break;

                default:

                    wc = new WebClient { Encoding = Encoding.UTF8 };
                    zap = string.Format("https://gdata.youtube.com/feeds/api/standardfeeds/{0}/most_popular?time=today&v=2&alt=json", cul);
                    res = wc.DownloadString(zap);
                    jsvideo = (JObject)JsonConvert.DeserializeObject(res);
                    if (jsvideo == null)
                        return;

                    foreach (JToken pair in jsvideo["feed"]["entry"])
                    {
                        var v = new VideoItemYou(pair, true, cul + " now") { Num = ListPopularVideoItems.Count + 1 };
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ListPopularVideoItems.Add(v);
                            v.IsHasFile = v.IsFileExist();
                            v.IsSynced = true;
                        });
                    }

                    #region all_time

                    //time=all_time
                    //zap = string.Format("https://gdata.youtube.com/feeds/api/standardfeeds/{0}/most_popular?time=all_time&v=2&alt=json", cul);
                    //s = wc.DownloadString(zap);
                    //jsvideo = (JObject)JsonConvert.DeserializeObject(s);
                    //if (jsvideo == null)
                    //    return;
                    //foreach (JToken pair in jsvideo["feed"]["entry"])
                    //{
                    //    var v = new VideoItemYou(pair, true, cul + " all") { Num = ListPopularVideoItems.Count + 1 };
                    //    Application.Current.Dispatcher.Invoke(() =>
                    //    {
                    //        ListPopularVideoItems.Add(v);
                    //        v.IsHasFile = v.IsFileExist();
                    //        v.IsSynced = true;
                    //    });
                    //}

                    #endregion

                    break;
            }
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _timer.Dispose();
            if (e.Result == null)
                return;
            switch (e.Result.ToString())
            {
                case "GetDB":
                    IsOnlyFavorites = Sqllite.GetSettingsIntValue(ChanelDb, "isonlyfavor") == 1;

                    if (IsSyncOnStart)
                        SyncChanel(IsOnlyFavorites ? "SyncChanelFavorites" : "SyncChanelAll");

                    if (IsPopular)
                    {
                        var culture = Sqllite.GetSettingsValue(ChanelDb, "culture");
                        _model.SelectedCountry = _model.Countries.First(x => x.Value == culture);
                    }
                    if (ChanelList.Any())
                        Result = string.Format("Chanells loaded in {0}", Synctime.Duration().ToString(@"mm\:ss"));
                    break;

                case "Search":
                    Result = string.Format("{0} searched in {1}", SearchKey, Synctime.Duration().ToString(@"mm\:ss"));
                    break;

                default:
                    Result = string.Format("{0} synced in {1}", _model.SelectedCountry.Key, Synctime.Duration().ToString(@"mm\:ss"));
                    break;
            }

        }

        private static void ChanelSync(ICollection list, bool isFull)
        {
            if (list == null || list.Count <= 0) return;

            foreach (ChanelBase chanel in list)
            {
                chanel.IsFull = isFull;
                chanel.GetItemsFromNet();
            }
        }

        private void FilterChanell()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChanelListToBind.Clear();
                if (IsOnlyFavorites)
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x => x.IsFavorite))
                    {
                        ChanelListToBind.Add(chanel);
                    }
                }
                else
                {
                    foreach (ChanelBase chanel in ChanelList)
                    {
                        ChanelListToBind.Add(chanel);
                    }
                }
                if (ChanelListToBind.Any())
                    CurrentChanel = ChanelListToBind[0];
            });
        }

        private void Filter()
        {
            if (string.IsNullOrEmpty(TitleFilter))
            {
                if (_filterlist.Any())
                {
                    ListPopularVideoItems.Clear();
                    foreach (VideoItemBase item in _filterlist)
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
                foreach (VideoItemBase item in _filterlist)
                {
                    if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                        ListPopularVideoItems.Add(item);
                }
            }
        }

        private void tmr_Tick(object o)
        {
            Synctime = Synctime.Add(TimeSpan.FromSeconds(1));
        }

        private void InitializeTimer()
        {
            Synctime = new TimeSpan();
            var tcb = new TimerCallback(tmr_Tick);
            _timer = new Timer(tcb, null, 0, 1000);
        }

        #endregion

        //public static void CheckFfmpegPath()
        //{
        //    if (string.IsNullOrEmpty(FfmpegPath))
        //        IsPathContainFfmpeg = false;
        //    else
        //    {
        //        var fn = new FileInfo(FfmpegPath);
        //        if (fn.Exists && fn.DirectoryName != null)
        //        {
        //            var winpath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
        //            if (winpath != null && winpath.Contains(fn.DirectoryName))
        //                IsPathContainFfmpeg = true;
        //            else
        //                IsPathContainFfmpeg = false;
        //        }
        //        else
        //            IsPathContainFfmpeg = false;
        //    }
        //}

        //public void ShowShutter(bool isShow)
        //{
        //    //this.Send(911); //show shutter
        //    //this.Send(910); //hide shutter
        //    this.Send(isShow ? 911 : 910);
        //}
    }
}
