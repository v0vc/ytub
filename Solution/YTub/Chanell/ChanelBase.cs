using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YTub.Common;
using YTub.Video;

namespace YTub.Chanell
{
    public abstract class ChanelBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion

        #region Fields

        private string _password;

        private string _login;

        private bool _isReady;

        private bool _isFavorite;

        private string _chanelName;

        public Timer TimerCommon;

        private IList _selectedListVideoItems = new ArrayList();

        private VideoItemBase _currentVideoItem;

        private string _titleFilter;

        private readonly List<VideoItemBase> _filterlist = new List<VideoItemBase>();

        public readonly BackgroundWorker Bgvdb = new BackgroundWorker();

        private string _lastColumnHeader;

        private string _viewSeedColumnHeader;

        private string _durationColumnHeader;

        #endregion

        #region Properties

        public TimeSpan Synctime { get; set; }

        public string Login
        {
            get { return _login; }
            set
            {
                _login = value;
                OnPropertyChanged("Login");
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }

        public string ChanelType { get; set; }

        public string ChanelOwner { get; set; }

        public string ChanelName
        {
            get { return _chanelName; }
            set
            {
                _chanelName = value;
                OnPropertyChanged("ChanelName");
            }
        }

        public string LastColumnHeader
        {
            get { return _lastColumnHeader; }
            set
            {
                _lastColumnHeader = value;
                OnPropertyChanged("LastColumnHeader");
            }
        }

        public string ViewSeedColumnHeader
        {
            get { return _viewSeedColumnHeader; }
            set
            {
                _viewSeedColumnHeader = value;
                OnPropertyChanged("ViewSeedColumnHeader");
            }
        }

        public string DurationColumnHeader
        {
            get { return _durationColumnHeader; }
            set
            {
                _durationColumnHeader = value;
                OnPropertyChanged("DurationColumnHeader");
            }
        }

        public int OrderNum { get; set; }

        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                _isReady = value;
                OnPropertyChanged("IsReady");
            }
        }

        public bool IsFull { get; set; }

        public TrulyObservableCollection<VideoItemBase> ListVideoItems { get; set; }

        public IList SelectedListVideoItems
        {
            get { return _selectedListVideoItems; }
            set
            {
                _selectedListVideoItems = value;
                OnPropertyChanged("SelectedListVideoItems");
            }
        }

        public VideoItemBase CurrentVideoItem
        {
            get { return _currentVideoItem; }
            set
            {
                _currentVideoItem = value;
                OnPropertyChanged("CurrentVideoItem");
            }
        }

        public bool IsFavorite
        {
            get { return _isFavorite; }
            set
            {
                _isFavorite = value;
                OnPropertyChanged("IsFavorite");
            }
        }

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

        #region Construction

        protected ChanelBase(string chaneltype, string login, string pass, string chanelname, string chanelowner,
            int ordernum)
        {
            ChanelType = chaneltype;
            Login = login;
            Password = pass;
            ChanelName = chanelname;
            ChanelOwner = chanelowner;
            OrderNum = ordernum;
            ListVideoItems = new TrulyObservableCollection<VideoItemBase>();
            Bgvdb.DoWork += _bgvdb_DoWork;
            Bgvdb.RunWorkerCompleted += _bgvdb_RunWorkerCompleted;
            var fn = new FileInfo(Subscribe.ChanelDb);
            if (fn.Exists)
            {
                var res = Sqllite.GetVideoIntValue(Subscribe.ChanelDb, "isfavorite", "chanelowner", ChanelOwner);
                IsFavorite = res != 0;
            }
        }

        #endregion

        #region Abstract Methods

        public abstract CookieContainer GetSession();

        public abstract void GetItemsFromNet();

        public abstract void AutorizeChanel();

        public abstract void DownloadItem();

        #endregion

        #region Public Methods

        public void GetItemsFromDb()
        {
            var res = Sqllite.GetChanelVideos(Subscribe.ChanelDb, ChanelOwner);
            foreach (DbDataRecord record in res)
            {
                VideoItemBase v = null;
                var servname = record["servername"].ToString();
                if (servname == "YouTube")
                    v = new VideoItemYou(record) {Num = ListVideoItems.Count + 1};
                if (servname == "RuTracker")
                    v = new VideoItemRt(record) {Num = ListVideoItems.Count + 1};
                if (servname == "Tapochek")
                    v = new VideoItemTap(record) {Num = ListVideoItems.Count + 1};
                if (v != null && !ListVideoItems.Contains(v))
                    ListVideoItems.Add(v);
            }
            var lst = new List<VideoItemBase>(ListVideoItems.Count);
            lst.AddRange(ListVideoItems);
            lst = lst.OrderByDescending(x => x.Published).ToList();
            ListVideoItems.Clear();
            foreach (VideoItemBase item in lst)
            {
                ListVideoItems.Add(item);
                item.Num = ListVideoItems.Count;
                item.IsHasFile = item.IsFileExist();
                //item.Delta = item.ViewCount - item.PrevViewCount;
            }
        }

        public static void WriteCookiesToDiskBinary(CookieContainer cookieJar, string filename)
        {
            var subs = ViewModelLocator.MvViewModel.Model.MySubscribe;
            var fn = new FileInfo(Path.Combine(Sqllite.AppDir, filename));
            if (fn.Exists)
            {
                try
                {
                    fn.Delete();
                }
                catch (Exception e)
                {
                    subs.Result = "WriteCookiesToDiskBinary: " + e.Message;
                }
            }
            using (Stream stream = File.Create(fn.FullName))
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                }
                catch (Exception e)
                {
                    subs.Result = "WriteCookiesToDiskBinary: " + e.Message;
                }
            }
        }

        public static CookieContainer ReadCookiesFromDiskBinary(string filename)
        {
            try
            {
                var fn = new FileInfo(Path.Combine(Sqllite.AppDir, filename));
                if (fn.Exists)
                {
                    using (Stream stream = File.Open(fn.FullName, FileMode.Open))
                    {
                        var formatter = new BinaryFormatter();
                        return (CookieContainer) formatter.Deserialize(stream);
                    }
                }
            }
            catch (Exception e)
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "ReadCookiesFromDiskBinary: " + e.Message;
            }
            return null;
        }

        public void AddToFavorites()
        {
            IsFavorite = !IsFavorite;
            var res = IsFavorite ? 1 : 0;
            Sqllite.UpdateValue(Subscribe.ChanelDb, "isfavorite", "chanelowner", ChanelOwner, res);
        }

        public void DeleteFiles()
        {
            if (SelectedListVideoItems.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (VideoItemBase item in SelectedListVideoItems)
                {
                    if (item.IsHasFile)
                        sb.Append(item.Title).Append(Environment.NewLine);
                }
                var result = MessageBox.Show("Are you sure to delete:" + Environment.NewLine + sb + "?", "Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    for (var i = SelectedListVideoItems.Count; i > 0; i--)
                    {
                        var video = SelectedListVideoItems[i - 1] as VideoItemBase;
                        if (video != null && video.IsHasFile)
                        {
                            var fn = new FileInfo(video.FilePath);
                            try
                            {
                                fn.Delete();
                                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "Deleted";
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            video.IsHasFile = false;
                            video.IsDownLoading = false;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select Video");
            }
        }

        #endregion

        #region Private Methods

        private void _bgvdb_DoWork(object sender, DoWorkEventArgs e)
        {
            switch ((int)e.Argument)
            {
                case 0: //новый канал

                    InsertItemToDb(ListVideoItems);

                    break;

                default: //данные уже есть в базе, надо обновить информацию

                    InsertItemToDb(ListVideoItems.Where(x => x.IsSynced == false).ToList()); //добавим только новые

                    if (IsFull) //в режиме Full - обновим показатели
                    {
                        foreach (VideoItemBase item in ListVideoItems)
                        {
                            Sqllite.UpdateValue(Subscribe.ChanelDb, "viewcount", "v_id", item.VideoID, item.ViewCount);
                        }
                    }
                    else //обновим только у последних элементов
                    {
                        foreach (VideoItemBase item in ListVideoItems.Take(25))
                        {
                            Sqllite.UpdateValue(Subscribe.ChanelDb, "viewcount", "v_id", item.VideoID, item.ViewCount);
                        }
                    }

                    break;
            }
        }

        private void _bgvdb_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var subs = ViewModelLocator.MvViewModel.Model.MySubscribe;
            if (e.Error == null)
            {
                TimerCommon.Dispose();
                var glSync = subs.Synctime;
                glSync = glSync.Add(Synctime.Duration());
                subs.Result = string.Format("Total: {0}. {1} synced in {2}", glSync.ToString(@"mm\:ss"), ChanelName,
                    Synctime.Duration().ToString(@"mm\:ss"));
            }
            else
            {
                subs.Result = e.Error.Message;
            }
        }

        private void Filter()
        {
            if (string.IsNullOrEmpty(TitleFilter))
            {
                if (_filterlist.Any())
                {
                    ListVideoItems.Clear();
                    foreach (VideoItemBase item in _filterlist)
                    {
                        if (item.Title.Contains(TitleFilter))
                            ListVideoItems.Add(item);
                    }
                }
            }
            else
            {
                if (!_filterlist.Any())
                    _filterlist.AddRange(ListVideoItems);
                ListVideoItems.Clear();
                foreach (VideoItemBase item in _filterlist)
                {
                    if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                        ListVideoItems.Add(item);
                }
            }
        }

        private void InsertItemToDb(IEnumerable<VideoItemBase> lstItems)
        {
            foreach (VideoItemBase item in lstItems)
            {
                if (item is VideoItemYou)
                {
                    #region Delta

                    //Вычисление дельты - сколько просмотров с предыдущей синхронизации, позволяет находить наиболее часто просматриваемые, но тормозит

                    //VideoItem item1 = item;
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    item1.Delta = item1.ViewCount - item1.PrevViewCount;
                    //    item1.PrevViewCount = Sqllite.GetVideoIntValue(Subscribe.ChanelDb, "viewcount", "v_id", item1.VideoID);
                    //});
                    //Sqllite.UpdateValue(Subscribe.ChanelDb, "previewcount", "v_id", item.VideoID, item.PrevViewCount);

                    #endregion

                    Sqllite.InsertRecord(Subscribe.ChanelDb, item.VideoID, ChanelOwner, ChanelName, ChanelType,
                        OrderNum, 0, item.VideoLink, item.Title, item.ViewCount, item.ViewCount, item.Duration,
                        item.Published, item.Description);
                    continue;
                }
                if (item is VideoItemRt)
                {
                    var rt = item as VideoItemRt;
                    Sqllite.InsertRecord(Subscribe.ChanelDb, item.VideoID, ChanelOwner, ChanelName, ChanelType,
                        OrderNum, 0, item.VideoLink, item.Title, item.ViewCount, rt.TotalDl, item.Duration,
                        item.Published, item.Description);
                }
            }
        }

        #endregion
    }
}
