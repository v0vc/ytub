using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SevenZip;
using YTub.Chanell;
using YTub.Controls;
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

        private readonly MainWindowModel _model;

        private bool _isOnlyFavorites;

        private string _result;

        private const string Dbfile = "ytub.db";

        private ChanelBase _currentChanel;

        private IList _selectedListChanels = new ArrayList();

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        private readonly List<VideoItemBase> _filterlist = new List<VideoItemBase>();

        private Timer _timer;

        private string _titleFilter;

        private string _chanelFilter;

        private string _searchKey;

        private int _selectedTabIndex;

        private ChanelBase _filterForumItem;

        private int _resCount;

        private ChanelBase _selectedForumItem;

        #endregion

        #region Properties

        public int ResCount
        {
            get { return _resCount; }
            set
            {
                _resCount = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        public ChanelBase CurrentChanel
        {
            get { return _currentChanel; }
            set
            {
                _currentChanel = value;
                OnPropertyChanged();
                if (_currentChanel != null)
                {
                    _model.MySubscribe.ResCount = _currentChanel.ListVideoItems.Count;
                }
            }
        }

        public ChanelBase SelectedForumItem
        {
            get { return _selectedForumItem; }
            set
            {
                _selectedForumItem = value;
                OnPropertyChanged();
            }
        }

        public ChanelBase FilterForumItem
        {
            get { return _filterForumItem; }
            set
            {
                _filterForumItem = value;
                OnPropertyChanged();
                FilterChannel();
            }
        }

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
                OnPropertyChanged();
            }
        }

        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnlyFavorites
        {
            get { return _isOnlyFavorites; }
            set
            {
                _isOnlyFavorites = value;
                OnPropertyChanged();
                if (ChanelList.Any())
                    FilterChannel();
            }
        }

        public string TitleFilter
        {
            get { return _titleFilter; }
            set
            {
                _titleFilter = value;
                OnPropertyChanged();
                FilterVideos();
            }
        }

        public string SearchKey
        {
            get { return _searchKey; }
            set
            {
                _searchKey = value;
                OnPropertyChanged();
            }
        }

        public string ChanelFilter
        {
            get { return _chanelFilter; }
            set
            {
                _chanelFilter = value;
                OnPropertyChanged();
                FilterChannel();
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
                    new ChanelYou("YouTube", string.Empty, string.Empty, "YouTube", string.Empty, 0, _model),
                    new ChanelRt("RuTracker", RtLogin, RtPass, "RuTracker", string.Empty, 0, _model),
                    new ChanelTap("Tapochek", TapLogin, TapPass, "Tapochek", string.Empty, 0, _model),
                    new ChanelEmpty()
                };
            }
            else
            {
                Result = "Ready";
                ServerList = new ObservableCollection<ChanelBase>
                {
                    new ChanelYou("YouTube", string.Empty, string.Empty, "YouTube", string.Empty, 0, _model),
                    new ChanelRt("RuTracker", string.Empty, string.Empty, "RuTracker", string.Empty, 0, _model),
                    new ChanelTap("Tapochek", string.Empty, string.Empty, "Tapochek", string.Empty, 0, _model),
                    new ChanelEmpty()
                };
                DownloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            SelectedForumItem = ServerList.First(x=>x is ChanelYou);
            FilterForumItem = ServerList.First(x => x is ChanelEmpty);
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        #endregion

        #region Public Methods

        public void PlayDownload(object obj)
        {
            if (obj == null)
                return;

            IList lsyou;
            switch (obj.ToString())
            {
                case "Search":

                    ChanelBase cnanel;
                    lsyou = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadItem(lsyou);
                    }

                    IList lsrt = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemRt>().Select(item => item).ToList();
                    if (lsrt.Count > 0)
                    {
                        cnanel = new ChanelRt(_model);
                        cnanel.DownloadItem(lsrt);
                    }

                    IList lstap = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemTap>().Select(item => item).ToList();
                    if (lstap.Count > 0)
                    {
                        cnanel = new ChanelTap(_model);
                        cnanel.DownloadItem(lstap);
                    }

                    break;

                case "Popular":

                    var chanelpop = new ChanelYou(_model);

                    chanelpop.DownloadItem(SelectedForumItem.SelectedListVideoItems);

                    break;

                case "Get":

                    CurrentChanel.DownloadItem(CurrentChanel.SelectedListVideoItems);

                    break;

                case "SearchPlay":
                case "PopularPlay":

                    if (SelectedForumItem.CurrentVideoItem is VideoItemYou)
                    {
                        var item = SelectedForumItem.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");

                    }
                    break;

                case "GetPlay":

                    if (CurrentChanel.CurrentVideoItem is VideoItemYou)
                    {
                        var item = CurrentChanel.CurrentVideoItem as VideoItemYou;
                        item.RunFile(item.IsHasFile ? "Local" : "Online");
                    }

                    break;

                case "GetInternal":

                    lsyou = CurrentChanel.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadVideoInternal(lsyou);
                    }

                    break;

                case "PopularInternal":
                case "SearchInternal":

                    lsyou = SelectedForumItem.SelectedListVideoItems.OfType<VideoItemYou>().Select(item => item).ToList();
                    if (lsyou.Count > 0)
                    {
                        cnanel = new ChanelYou(_model);
                        cnanel.DownloadVideoInternal(lsyou);
                    }

                    break;
            }
        }

        public void AddChanel(object o)
        {
            var isEdit = o != null && o.ToString() == "edit";
            try
            {
                var servlist = new ObservableCollection<ChanelBase>(ServerList.Where(x => x.ChanelType != "All"));
                var addChanelModel = new AddChanelModel(_model, null, isEdit, servlist);
                if (isEdit)
                {
                    addChanelModel.ChanelOwner = CurrentChanel.ChanelOwner;
                    addChanelModel.ChanelName = ChanelBase.ChanellClearName(CurrentChanel.ChanelName);
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

        public void AddChanell()
        {
            var ordernum = _model.MySubscribe.ChanelList.Count;
            var item = _model.MySubscribe.SelectedForumItem.CurrentVideoItem;
            if (!_model.MySubscribe.ChanelList.Select(z => z.ChanelOwner).Contains(item.VideoOwner))
            {
                ChanelBase chanel = null;
                if (item is VideoItemYou)
                    chanel = new ChanelYou(item.ServerName, RtLogin, RtPass, item.VideoOwner, item.VideoOwner, ordernum, _model);

                if (item is VideoItemRt)
                    chanel = new ChanelRt(item.ServerName, RtLogin, RtPass, item.VideoOwnerName, item.VideoOwner, ordernum, _model);

                if (item is VideoItemTap)
                    chanel = new ChanelTap(item.ServerName, TapLogin, TapPass, item.VideoOwnerName, item.VideoOwner, ordernum, _model);

                if (chanel != null)
                {
                    _model.MySubscribe.ChanelList.Add(chanel);
                    _model.MySubscribe.ChanelListToBind.Add(chanel);
                    chanel.IsFull = true;
                    chanel.GetItemsFromNet();
                    _model.MySubscribe.CurrentChanel = chanel;
                    _model.MySubscribe.SelectedTabIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("Subscribe has already " + item.VideoOwner, "Information", MessageBoxButton.OK,
                    MessageBoxImage.Information);
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
                        FilterChannel();
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
                _bgv.RunWorkerAsync();
        }

        public void GetPopularVideos(string culture)
        {
            SelectedForumItem.ListPopularVideoItems.Clear();
            if (SelectedForumItem is ChanelYou)
            {
                (SelectedForumItem as ChanelYou).GetPopularItems(culture, SelectedForumItem.ListPopularVideoItems);
            }
            else
            {
                var chanell = new ChanelYou(_model);
                chanell.GetPopularItems(culture, SelectedForumItem.ListPopularVideoItems);    
            }
        }

        public void SearchItems(object obj)
        {
            if (string.IsNullOrEmpty(SearchKey))
                return;

            SelectedForumItem.ListSearchVideoItems.Clear();
            if (SelectedForumItem is ChanelYou)
            {
                (SelectedForumItem as ChanelYou).SearchItems(SearchKey, SelectedForumItem.ListSearchVideoItems);
            }

            if (SelectedForumItem is ChanelRt)
            {
                var chanel = SelectedForumItem as ChanelRt;
                chanel.IsFull = true;
                chanel.SearchItems(SearchKey, chanel.ListSearchVideoItems);
            }

            if (SelectedForumItem is ChanelTap)
            {
                var chanel = SelectedForumItem as ChanelTap;
                chanel.IsFull = true;
                chanel.SearchItems(SearchKey, chanel.ListSearchVideoItems);
            }
        }

        public void SyncChanel(object obj)
        {
            Result = string.Empty;
            Synctime = new TimeSpan();

            Task.Run(() =>
            {
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
            });
        }

        public static void SetResult(string result)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.Result = result;
        }

        #endregion

        #region Private Methods

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (KeyValuePair<string, string> pair in Sqllite.GetDistinctValues(ChanelDb, "chanelowner", "chanelname"))
            {
                var sp = pair.Value.Split(':');

                ChanelBase chanel = null;
                if (sp[1] == "YouTube")
                    chanel = new ChanelYou(sp[1], "TODO", "TODO", sp[0], pair.Key, Convert.ToInt32(sp[2]), _model);
                if (sp[1] == "RuTracker")
                    chanel = new ChanelRt(sp[1], RtLogin, RtPass, sp[0], pair.Key, Convert.ToInt32(sp[2]), _model);
                if (sp[1] == "Tapochek")
                    chanel = new ChanelTap(sp[1], TapLogin, TapPass, sp[0], pair.Key, Convert.ToInt32(sp[2]), _model);

                ChanelList.Add(chanel);
            }

            foreach (ChanelBase chanel in ChanelList)
            {
                chanel.GetItemsFromDb();
            }

            if (ChanelList.Any())
            {
                CurrentChanel = ChanelList[0];
            }
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _timer.Dispose();
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

        private void FilterChannel()
        {
            if (Application.Current.Dispatcher.CheckAccess())
                FilterChannelCore();
            else
                Application.Current.Dispatcher.Invoke(FilterChannelCore);
        }

        private void FilterVideos()
        {
            if (Application.Current.Dispatcher.CheckAccess())
                FilterVideosCore();
            else
                Application.Current.Dispatcher.Invoke(FilterVideosCore);
        }

        private void FilterChannelCore()
        {
            ChanelListToBind.Clear();
            if (IsOnlyFavorites)
            {
                if (FilterForumItem is ChanelEmpty)
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x => x.IsFavorite))
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }
                }
                else
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x => x.IsFavorite & x.ChanelType == FilterForumItem.ChanelType))
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }
                }
            }
            else
            {
                if (FilterForumItem is ChanelEmpty)
                {
                    foreach (ChanelBase chanel in ChanelList)
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }    
                }
                else
                {
                    foreach (ChanelBase chanel in ChanelList.Where(x=>x.ChanelType == FilterForumItem.ChanelType))
                    {
                        if (string.IsNullOrEmpty(ChanelFilter))
                            ChanelListToBind.Add(chanel);
                        else
                        {
                            if (chanel.ChanelName.ToUpper().Contains(ChanelFilter.ToUpper()))
                                ChanelListToBind.Add(chanel);
                        }
                    }    
                }
            }
            
            if (ChanelListToBind.Any())
                CurrentChanel = ChanelListToBind[0];
        }

        private void FilterVideosCore()
        {
            if (string.IsNullOrEmpty(TitleFilter))
            {
                if (_filterlist.Any())
                {
                    SelectedForumItem.ListPopularVideoItems.Clear();
                    foreach (VideoItemBase item in _filterlist)
                    {
                        if (item.Title.Contains(TitleFilter))
                            SelectedForumItem.ListPopularVideoItems.Add(item);
                    }
                }
            }
            else
            {
                if (!_filterlist.Any())
                    _filterlist.AddRange(SelectedForumItem.ListPopularVideoItems);
                SelectedForumItem.ListPopularVideoItems.Clear();
                foreach (VideoItemBase item in _filterlist)
                {
                    if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                        SelectedForumItem.ListPopularVideoItems.Add(item);
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
