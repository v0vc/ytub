using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YoutubeExtractor;
using YTub.Common;
using YTub.Video;

namespace YTub.Chanell
{
    public class ChanelYou : ChanelBase
    {
        private int _step;

        private readonly List<VideoItemBase> _selectedListVideoItemsList = new List<VideoItemBase>();

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        public int MinRes { get; set; }
        public int MaxResults { get; set; }

        public ChanelYou(string chaneltype, string login, string pass, string chanelname, string chanelowner, int ordernum) : base(chaneltype, login, pass, chanelname, chanelowner, ordernum)
        {
            MinRes = 1;
            MaxResults = 25;
            LastColumnHeader = "Download";
            ViewSeedColumnHeader = "Views";
            DurationColumnHeader = "Duration";
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public override void GetItemsFromNet()
        {
            if (_bgv.IsBusy)
                return;
            Synctime = new TimeSpan();
            ViewModelLocator.MvViewModel.Model.MySubscribe.Synctime = Synctime;
            var tcb = new TimerCallback(tmr_Tick);
            TimerCommon = new Timer(tcb, null, 0, 1000);
            if (IsFull)
                ListVideoItems.Clear();
            _bgv.RunWorkerAsync();
        }

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                var wc = new WebClient { Encoding = Encoding.UTF8 };
                var zap = string.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads?alt=json&start-index={1}&max-results={2}",
                    ChanelOwner, MinRes, MaxResults);
                string s = wc.DownloadString(zap);
                var jsvideo = (JObject)JsonConvert.DeserializeObject(s);
                if (jsvideo == null)
                    return;
                int total;
                if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                {
                    foreach (JToken pair in jsvideo["feed"]["entry"])
                    {
                        var v = new VideoItemYou(pair, false, "RU")
                        {
                            Num = ListVideoItems.Count + 1,
                            VideoOwner = ChanelOwner
                        };

                        if (IsFull)
                        {
                            if (ListVideoItems.Contains(v) || string.IsNullOrEmpty(v.Title))
                                continue;
                            Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
                        }
                        else
                        {
                            if (ListVideoItems.Select(x => x.VideoID).Contains(v.VideoID) || string.IsNullOrEmpty(v.Title))
                                continue;
                            Application.Current.Dispatcher.Invoke(() => ListVideoItems.Insert(0, v));
                        }

                        //Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
                    }
                    if (!IsFull)
                    {
                        for (int i = 0; i < ListVideoItems.Count; i++)
                        {
                            var k = i;
                            ListVideoItems[i].Num = k + 1;
                        }
                        return;
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

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
                    foreach (VideoItemBase item in ListVideoItems)
                    {
                        item.IsHasFile = item.IsFileExist();
                    }
                }
                else
                {
                    foreach (VideoItemBase item in ListVideoItems)
                    {
                        item.IsHasFile = item.IsFileExist();
                        item.IsSynced = Sqllite.IsTableHasRecord(Subscribe.ChanelDb, item.VideoID);
                    }
                    IsReady = !ListVideoItems.Select(x => x.IsSynced).Contains(false);
                }
                if (!Bgvdb.IsBusy)
                    Bgvdb.RunWorkerAsync(totalrow); //отдельный воркер для записи в базу
            }
        }

        public override CookieContainer GetSession()
        {
            return null;
        }

        public override void AutorizeChanel()
        {
        }

        public override void DownloadItem()
        {
            if (string.IsNullOrEmpty(Subscribe.YoudlPath))
            {
                MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Subscribe.IsAsyncDl)
                GetVideosASync();
            else
                GetVideosSync();
        }

        #region YouTubeExtractor
        public async void DownloadVideoInternal()
        {
            var lst = new List<VideoItemBase>(SelectedListVideoItems.Count);
            lst.AddRange(SelectedListVideoItems.Cast<VideoItemBase>());
            foreach (VideoItemBase item in lst)
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
            CurrentVideoItem.IsHasFile = CurrentVideoItem.IsFileExist();
        }

        private static Task DownloadVideoAsync(VideoItemBase item)
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

                    var downloader = new VideoDownloader(videoInfo, Path.Combine(Subscribe.DownloadPath, item.VideoOwner, VideoItemBase.MakeValidFileName(videoInfo.Title) + videoInfo.VideoExtension));

                    downloader.DownloadProgressChanged += (sender, args) => downloader_DownloadProgressChanged(args, item);
                    downloader.DownloadFinished += delegate { downloader_DownloadFinished(downloader, item); };
                    downloader.Execute();
                }
            });
        }

        private static void downloader_DownloadFinished(object sender, VideoItemBase o)
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

        private static void downloader_DownloadProgressChanged(ProgressEventArgs e, VideoItemBase o)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                o.PercentDownloaded = e.ProgressPercentage;
            }));
        } 
        #endregion

        private void GetVideosASync()
        {
            foreach (VideoItemBase item in SelectedListVideoItems)
            {
                YouWrapper youwr;
                if (!string.IsNullOrEmpty(item.VideoOwner))
                    youwr = new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Path.Combine(Subscribe.DownloadPath, item.VideoOwner), item);
                else
                    youwr = new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Subscribe.DownloadPath, item);

                youwr.DownloadFile(false);
            }
        }

        private void GetVideosSync()
        {
            _selectedListVideoItemsList.Clear();
            foreach (VideoItemBase item in SelectedListVideoItems)
            {
                _selectedListVideoItemsList.Add(item);
            }

            GetVideos();
        }

        private void GetVideos()
        {
            if (SelectedListVideoItems.Count == 1)
                _step = 0;
            YouWrapper youwr = !string.IsNullOrEmpty(_selectedListVideoItemsList[_step].VideoOwner)
                ? new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Path.Combine(Subscribe.DownloadPath, _selectedListVideoItemsList[_step].VideoOwner), _selectedListVideoItemsList[_step])
                : new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Subscribe.DownloadPath, _selectedListVideoItemsList[_step]);
            youwr.Activate += youwr_nextstep;
            youwr.DownloadFile(false);
            _step++;
        }

        private void youwr_nextstep()
        {
            if (_step < _selectedListVideoItemsList.Count)
                GetVideos();
        }

        private void tmr_Tick(object o)
        {
            Synctime = Synctime.Add(TimeSpan.FromSeconds(1));
        }

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
    }
}
