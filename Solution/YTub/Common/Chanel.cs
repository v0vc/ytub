using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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

        private string _chanelName;

        private IList _selectedListVideoItems = new ArrayList();

        private VideoItem _currentVideoItem;

        private string _result;

        public int MaxResults { get; set; }

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

        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                OnPropertyChanged("Result");
            }
        }

        public Chanel(string name, string user)
        {
            MaxResults = 25;
            MinRes = 1;
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("Chanel user must be set");

            if (string.IsNullOrEmpty(name))
                name = user;

            ChanelName = name;
            ChanelOwner = user;
            ListVideoItems = new TrulyObservableCollection<VideoItem>();
        }

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
                                Result = "Deleted";
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

        public void GetChanelVideoItems(int minres)
        {
            Application.Current.Dispatcher.Invoke(() => ListVideoItems.Clear());
            while (true)
            {
                var wc = new WebClient {Encoding = Encoding.UTF8};
                var zap = string.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads?alt=json&start-index={1}&max-results={2}", ChanelOwner, minres, MaxResults);
                string s = wc.DownloadString(zap);
                var jsvideo = (JObject) JsonConvert.DeserializeObject(s);
                if (jsvideo == null)
                    return;
                int total;
                if (int.TryParse(jsvideo["feed"]["openSearch$totalResults"]["$t"].ToString(), out total))
                {
                    foreach (JToken pair in jsvideo["feed"]["entry"])
                    {
                        var v = new VideoItem(pair) {Num = ListVideoItems.Count + 1, VideoOwner = ChanelOwner};
                        Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
                    }
                    if (total > ListVideoItems.Count)
                    {
                        minres = minres + MaxResults;
                        continue;
                    }
                }

                break;
            }
            
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
                //if (item.Num % 2 == 1)
                //    item.IsHasFile = true;
                ListVideoItems.Add(item);
                item.Num = ListVideoItems.Count;
                item.IsHasFile = item.IsFileExist(item);
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
            Result = "Download Completed";
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
                    o.IsHasFile = true;
                    o.FilePath = vd.SavePath;
                }));
            }
        }

        private static void downloader_DownloadProgressChanged(ProgressEventArgs e, VideoItem o)
        {
            if ((int)e.ProgressPercentage%20 == 0)
            {
                Application.Current.Dispatcher.BeginInvoke((Action) (() =>
                {
                    o.PercentDownloaded = e.ProgressPercentage;
                }));
            }
        }

        public void DownloadVideoExternal()
        {
            if (string.IsNullOrEmpty(Subscribe.YoudlPath))
            {
                MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (VideoItem item in SelectedListVideoItems)
            {
                bool isok;
                YouWrapper.DownloadFile(Subscribe.YoudlPath, item.VideoLink, Path.Combine(Subscribe.DownloadPath, item.VideoOwner), item.ClearTitle, out isok);
                if (isok)
                    item.IsHasFile = true;
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
