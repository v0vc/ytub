using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
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
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YoutubeExtractor;

namespace YTub.Common
{
    public class Chanel :INotifyPropertyChanged
    {
        private bool _isReady;

        private bool _isFavorite;

        private string _chanelName;

        private IList _selectedListVideoItems = new ArrayList();

        private VideoItem _currentVideoItem;
        
        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        private readonly BackgroundWorker _bgvdb = new BackgroundWorker();

        private Timer _timer;

        private string _titleFilter;

        private readonly List<VideoItem> _filterlist = new List<VideoItem>();

        #region Fields
        public int MaxResults { get; set; }

        public TimeSpan Synctime;
        public int MinRes { get; set; }

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

        public string ServerName { get; set; }

        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                _isReady = value;
                OnPropertyChanged("IsReady");
            }
        }

        public TrulyObservableCollection<VideoItem> ListVideoItems { get; set; }

        public IList SelectedListVideoItems
        {
            get { return _selectedListVideoItems; }
            set
            {
                _selectedListVideoItems = value;
                OnPropertyChanged("SelectedListVideoItems");
            }
        }

        public VideoItem CurrentVideoItem
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

        public Chanel(string name, string user, string servername)
        {
            MaxResults = 25;
            MinRes = 1;
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("Chanel user must be set");

            if (string.IsNullOrEmpty(name))
                name = user;

            ChanelName = name;
            ChanelOwner = user;
            ServerName = servername;
            var res = Sqllite.GetVideoIntValue(Subscribe.ChanelDb, "isfavorite", "chanelowner", ChanelOwner);
            IsFavorite = res != 0;
            ListVideoItems = new TrulyObservableCollection<VideoItem>();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            _bgvdb.DoWork += _bgvdb_DoWork;
            _bgvdb.RunWorkerCompleted += _bgvdb_RunWorkerCompleted;
        }

        private void Filter()
        {
            if (string.IsNullOrEmpty(TitleFilter))
            {
                if (_filterlist.Any())
                {
                    ListVideoItems.Clear();
                    foreach (VideoItem item in _filterlist)
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
                foreach (VideoItem item in _filterlist)
                {
                    if (item.Title.ToLower().Contains(TitleFilter.ToLower()))
                        ListVideoItems.Add(item);
                }
            }
        }

        public void AddToFavorites()
        {
            IsFavorite = !IsFavorite;
            var res = IsFavorite ? 1 : 0;
            Sqllite.UpdateValue(Subscribe.ChanelDb, "isfavorite", "chanelowner", ChanelOwner, res);
        }

        void _bgvdb_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                _timer.Dispose();
                ViewModelLocator.MvViewModel.Model.MySubscribe.Synctime =
                    ViewModelLocator.MvViewModel.Model.MySubscribe.Synctime.Add(Synctime.Duration());
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = string.Format("Total: {0}. {1} synced in {2}",
                    ViewModelLocator.MvViewModel.Model.MySubscribe.Synctime.ToString(@"mm\:ss"), ChanelName,
                    Synctime.Duration().ToString(@"mm\:ss"));
            }
            else
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = e.Error.Message;
            }
        }

        void _bgvdb_DoWork(object sender, DoWorkEventArgs e)
        {
            switch ((int)e.Argument)
            {
                case 0: //новый канал

                    foreach (VideoItem videoItem in ListVideoItems)
                    {
                        Sqllite.InsertRecord(Subscribe.ChanelDb, videoItem.VideoID, ChanelOwner, ChanelName, ServerName, 0, videoItem.VideoLink, videoItem.Title, videoItem.ViewCount, videoItem.ViewCount, videoItem.Duration, videoItem.Published, videoItem.Description);
                    }

                    break;

                default: //данные уже есть в базе, надо обновить информацию

                    foreach (VideoItem item in ListVideoItems)
                    {
                        //Вычисление дельты - сколько просмотров с предыдущей синхронизации, позволяет находить наиболее часто просматриваемые, но тормозит

                        //VideoItem item1 = item;
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                        //    item1.Delta = item1.ViewCount - item1.PrevViewCount;
                        //    item1.PrevViewCount = Sqllite.GetVideoIntValue(Subscribe.ChanelDb, "viewcount", "v_id", item1.VideoID);
                        //});
                        //Sqllite.UpdateValue(Subscribe.ChanelDb, "previewcount", "v_id", item.VideoID, item.PrevViewCount);

                        Sqllite.UpdateValue(Subscribe.ChanelDb, "viewcount", "v_id", item.VideoID, item.ViewCount);
                        if (item.IsSynced == false)
                            Sqllite.InsertRecord(Subscribe.ChanelDb, item.VideoID, ChanelOwner, ChanelName, ServerName, 0,
                                item.VideoLink, item.Title, item.ViewCount, item.ViewCount, item.Duration,
                                item.Published, item.Description);
                    }

                    break;
            }
        }

        void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
            else
            {
                MinRes = 1;
                var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                if (dir == null) return;
                int totalrow;
                Sqllite.CreateOrConnectDb(Subscribe.ChanelDb, ChanelOwner, out totalrow);
                if (totalrow == 0)
                {
                    foreach (VideoItem item in ListVideoItems)
                    {
                        item.IsHasFile = item.IsFileExist(item);
                    }
                }
                else
                {
                    foreach (VideoItem item in ListVideoItems)
                    {
                        item.IsHasFile = item.IsFileExist(item);
                        item.IsSynced = Sqllite.IsTableHasRecord(Subscribe.ChanelDb, item.VideoID);
                    }
                    IsReady = !ListVideoItems.Select(x => x.IsSynced).Contains(false);
                }
                if (!_bgvdb.IsBusy)
                    _bgvdb.RunWorkerAsync(totalrow); //отдельный воркер для записи в базу
            }
        }

        void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                var wc = new WebClient { Encoding = Encoding.UTF8 };
                var zap = string.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads?alt=json&start-index={1}&max-results={2}", ChanelOwner, MinRes, MaxResults);
                string s = wc.DownloadString(zap);
                var jsvideo = (JObject)JsonConvert.DeserializeObject(s);
                if (jsvideo == null)
                    return;
                int total;
                if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                {
                    foreach (JToken pair in jsvideo["feed"]["entry"])
                    {
                        var v = new VideoItem(pair, false, "RU") { Num = ListVideoItems.Count + 1, VideoOwner = ChanelOwner };
                        Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
                    }
                    if (total > ListVideoItems.Count)
                    {
                        MinRes = MinRes + MaxResults;
                        continue;
                    }
                }
                break;
            } 
        }

        public void DeleteFiles()
        {
            if (SelectedListVideoItems.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (VideoItem item in SelectedListVideoItems)
                {
                    if (item.IsHasFile)
                        sb.Append(item.Title).Append(Environment.NewLine);
                }
                var result = MessageBox.Show("Are you sure to delete:" + Environment.NewLine + sb + "?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    for (var i = SelectedListVideoItems.Count; i > 0; i--)
                    {
                        var video = SelectedListVideoItems[i - 1] as VideoItem;
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

        public void GetChanelVideoItems()
        {
            Synctime = new TimeSpan();
            ViewModelLocator.MvViewModel.Model.MySubscribe.Synctime = Synctime;
            var tcb = new TimerCallback(tmr_Tick);
            _timer = new Timer(tcb, null, 0, 1000);
            ListVideoItems.Clear();
            _bgv.RunWorkerAsync();
        }

        public void GetChanelVideoItemsFromDb(string dbfile)
        {
            var res = Sqllite.GetChanelVideos(dbfile, ChanelOwner);
            foreach (DbDataRecord record in res)
            {
                var v = new VideoItem(record) {Num = ListVideoItems.Count + 1};
                ListVideoItems.Add(v);
            }
            var lst = new List<VideoItem>(ListVideoItems.Count);
            lst.AddRange(ListVideoItems);
            lst = lst.OrderByDescending(x => x.Published).ToList();
            ListVideoItems.Clear();
            foreach (VideoItem item in lst)
            {
                ListVideoItems.Add(item);
                item.Num = ListVideoItems.Count;
                item.IsHasFile = item.IsFileExist(item);
                //item.Delta = item.ViewCount - item.PrevViewCount;
            }
        }

        public async void DownloadVideoInternal()
        {
            var lst = new List<VideoItem>(SelectedListVideoItems.Count);
            lst.AddRange(SelectedListVideoItems.Cast<VideoItem>());
            foreach (VideoItem item in lst)
            {
                CurrentVideoItem = item;
                var dir = new DirectoryInfo(Path.Combine(Subscribe.DownloadPath, CurrentVideoItem.VideoOwner));
                if (!dir.Exists)
                    dir.Create();
                CurrentVideoItem.IsDownLoading = true;
                CurrentVideoItem.IsHasFile = false;
                await DownloadVideoAsync(CurrentVideoItem);
            }
            ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "Download Completed";
        }

        private static Task DownloadVideoAsync(VideoItem item)
        {
            return Task.Run(() =>
            {
                IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(item.VideoLink).OrderByDescending(z => z.Resolution);
                VideoInfo videoInfo = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.AudioBitrate != 0);
                if (videoInfo != null)
                {
                    if (videoInfo.RequiresDecryption)
                    {
                        DownloadUrlResolver.DecryptDownloadUrl(videoInfo);
                    }

                    var downloader = new VideoDownloader(videoInfo, Path.Combine(Subscribe.DownloadPath, item.VideoOwner, VideoItem.MakeValidFileName(videoInfo.Title) + videoInfo.VideoExtension));

                    downloader.DownloadProgressChanged += (sender, args) => downloader_DownloadProgressChanged(args, item);
                    downloader.DownloadFinished += delegate {downloader_DownloadFinished(downloader, item);};
                    downloader.Execute();
                }
            });
        }

        private static void downloader_DownloadFinished(object sender, VideoItem o)
        {
            var vd = sender as VideoDownloader;
            if (vd != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    //o.IsHasFile = true;
                    o.FilePath = vd.SavePath;
                }));
            }
        }

        private static void downloader_DownloadProgressChanged(ProgressEventArgs e, VideoItem o)
        {
            Application.Current.Dispatcher.BeginInvoke((Action) (() =>
            {
                o.PercentDownloaded = e.ProgressPercentage;
            }));
        }

        public void DownloadVideoExternal()
        {
            if (string.IsNullOrEmpty(Subscribe.YoudlPath))
            {
                MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //Subscribe.CheckFfmpegPath();

            foreach (VideoItem item in SelectedListVideoItems)
            {
                YouWrapper youwr;
                if (!string.IsNullOrEmpty(item.VideoOwner))
                    youwr = new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Path.Combine(Subscribe.DownloadPath, item.VideoOwner), item);
                        //, Subscribe.IsPathContainFfmpeg);
                else
                    youwr = new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Subscribe.DownloadPath, item);//, Subscribe.IsPathContainFfmpeg);
                youwr.DownloadFile(false);
            }
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

        #region regex

        //public void GetChanelVideoItemsWithoutGoogle()
        //{
        //    ListVideoItems.Clear();
        //    var web = new HtmlWeb
        //    {
        //        AutoDetectEncoding = false,
        //        OverrideEncoding = Encoding.UTF8,
        //    };
        //    var chanelDoc = web.Load(ChanelLink.AbsoluteUri);
        //    if (chanelDoc == null)
        //        throw new HtmlWebException("Can't load page: " + Environment.NewLine + ChanelLink.AbsoluteUri);
        //    //var i = 0;
        //    foreach (HtmlNode link in chanelDoc.DocumentNode.SelectNodes("//a[@href]"))
        //    {
        //        var att = link.Attributes["href"];
        //        string parsed;
        //        if (!IsLinkCorrectYouTube(att.Value, out parsed))
        //            continue;
        //        var parsedtrim = parsed.TrimEnd('&');
        //        var sp = parsedtrim.Split('=');
        //        if (sp.Length == 2 && sp[1].Length == 11)
        //        {
        //            var v = new VideoItem(parsedtrim, sp[1]);
        //            //var removedBreaksname = link.InnerText.Trim().Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        //            //v.VideoName = removedBreaksname;
        //            if (!ListVideoItems.Select(x => x.RawUrl).ToList().Contains(v.RawUrl))
        //            {
        //                ListVideoItems.Add(v);
        //                //i++;
        //            }
        //        }
        //    }
        //}

        //private static bool IsLinkCorrectYouTube(string input, out string parsedres)
        //{
        //    var res = false;
        //    parsedres = string.Empty;
        //    var regExp = new Regex(@"(watch\?.)(.+?)(?:[^-a-zA-Z0-9]|$)");
        //    //var regExp = new Regex(@"(?:youtu\.be\/|youtube.com\/(?:watch\?.*\bv=|embed\/|v\/)|ytimg\.com\/vi\/)(.+?)(?:[^-a-zA-Z0-9]|$)");
        //    //var regExp = new Regex(@"/^.*((youtu.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#\&\?]*).*/");
        //    //var regExp = new Regex(@"/^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/");
        //    //var regExp = new Regex(@"/(?:https?://)?(?:www\.)?youtu(?:be\.com/watch\?(?:.*?&(?:amp;)?)?v=|\.be/)([\w‌​\-]+)(?:&(?:amp;)?[\w\?=]*)?/");
        //    //var regExp = new Regex(@"http://(?:www\.)?youtu(?:be\.com/watch\?v=|\.be/)(\w*)(&(amp;)?[\w\?=]*)?");
        //    //var regExp = new Regex(@"(?:(?:watch\?.*\bv=|embed\/|v\/)|ytimg\.com\/vi\/)(.+?)(?:[^-a-zA-Z0-9]|$)");
        //    var match = regExp.Match(input);
        //    if (match.Success)
        //    {
        //        parsedres = match.Value;
        //        res = true;
        //    }
        //    return res;
        //}

        #endregion
    }
}
