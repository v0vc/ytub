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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using YTub.Common;
using YTub.Controls;
using YTub.Models;
using YTub.Video;

namespace YTub.Chanell
{
    public class ChanelRt :ChanelBase
    {
        private readonly MainWindowModel _model;

        private const string Cname = "rtcookie.ck";

        private const string UrlUserBase = "http://rutracker.org/forum/tracker.php?rid";

        private const string UrlSearchBase = "http://rutracker.org/forum/tracker.php?nm";

        private CookieContainer _rtcookie;

        private ObservableCollectionEx<VideoItemBase> _listSearchVideoItems;

        //private TrulyObservableCollection<VideoItemBase> _listPopularVideoItems;

        private string _searchkey;

        private readonly BackgroundWorker _bgv = new BackgroundWorker();

        public ChanelRt(string chaneltype, string login, string pass, string chanelname, string chanelowner, int ordernum, MainWindowModel model) : base(chaneltype, login, pass, chanelname, chanelowner, ordernum, model)
        {
            _model = model;
            LastColumnHeader = "Total DL";
            ViewSeedColumnHeader = "Seeders";
            DurationColumnHeader = "Size MB";
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public ChanelRt(MainWindowModel model)
        {
            _model = model;
            LastColumnHeader = "Total DL";
            ViewSeedColumnHeader = "Seeders";
            DurationColumnHeader = "Size MB";
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
        }

        public override void GetItemsFromNet()
        {
            if (_bgv.IsBusy)
                return;
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
            {
                Subscribe.SetResult("Please, set Login and Password");
                return;
            }
            
            InitializeTimer();

            if (IsFull)
                ListVideoItems.Clear();
            _rtcookie = ReadCookiesFromDiskBinary(Cname);
            if (_rtcookie == null)
                AutorizeChanel();
            _bgv.RunWorkerAsync("Get");
        }

        public override CookieContainer GetSession()
        {
            try
            {
                var cc = new CookieContainer();
                var req = (HttpWebRequest)WebRequest.Create("http://login.rutracker.org/forum/login.php");
                req.CookieContainer = cc;
                req.Method = WebRequestMethods.Http.Post;
                req.Host = "login.rutracker.org";
                req.KeepAlive = true;
                var postData = string.Format("login_username={0}&login_password={1}&login=%C2%F5%EE%E4", Uri.EscapeDataString(Login), Uri.EscapeDataString(Password));
                var data = Encoding.ASCII.GetBytes(postData);
                req.ContentLength = data.Length;
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Headers.Add("Cache-Control", "max-age=0");
                req.Headers.Add("Origin", @"http://rutracker.org");
                req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                req.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                req.Headers.Add("DNT", "1");
                req.Referer = @"http://rutracker.org/forum/index.php";

                using (var stream = req.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var resp = (HttpWebResponse)req.GetResponse();
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    cc.Add(resp.Cookies);
                }
                return cc;
            }
            catch (Exception ex)
            {
                Subscribe.SetResult(ex.Message);
            }
            return null;
        }

        public override void AutorizeChanel()
        {
            _rtcookie = GetSession();
            if (_rtcookie.Count > 0)
                WriteCookiesToDiskBinary(_rtcookie, Cname);
        }

        public override void DownloadItem(IList list)
        {
            _rtcookie = ReadCookiesFromDiskBinary(Cname) ?? GetSession();
            // Construct HTTP request to get the file
            var httpRequest = (HttpWebRequest)WebRequest.Create(CurrentVideoItem.VideoLink);
            httpRequest.Method = WebRequestMethods.Http.Post;
            foreach (VideoItemBase item in list)
            {
                httpRequest.Referer = string.Format("http://rutracker.org/forum/viewtopic.php?t={0}", item.VideoID);
                httpRequest.CookieContainer = _rtcookie;

                // Include post data in the HTTP request
                const string postData = "dummy=";
                httpRequest.ContentLength = postData.Length;
                httpRequest.ContentType = "application/x-www-form-urlencoded";

                // Write the post data to the HTTP request
                var requestWriter = new StreamWriter(httpRequest.GetRequestStream(), Encoding.ASCII);
                requestWriter.Write(postData);
                requestWriter.Close();

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                Stream httpResponseStream = httpResponse.GetResponseStream();

                const int bufferSize = 1024;
                var buffer = new byte[bufferSize];

                // Read from response and write to file
                var ddir = new DirectoryInfo(Path.Combine(Subscribe.DownloadPath, string.Format("rt-{0}({1})", ChanellClearName(item.VideoOwnerName), item.VideoOwner)));
                if (!ddir.Exists)
                    ddir.Create();

                var rt = item as VideoItemRt;
                if (rt != null)
                {
                    var dpath = VideoItemBase.AviodTooLongFileName(Path.Combine(ddir.FullName, rt.MakeTorrentFileName(false)));
                    FileStream fileStream = File.Create(dpath);
                    int bytesRead;
                    while (httpResponseStream != null && (bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    } // end while
                    var fn = new FileInfo(dpath);
                    if (fn.Exists)
                        rt.FilePath = fn.FullName;

                    rt.IsHasFile = fn.Exists;
                }
                _model.MySubscribe.Result = item.Title + " downloaded";
                break; //TODO mass torr dl
            }
        }

        public override void SearchItems(string key, ObservableCollectionEx<VideoItemBase> listSearchVideoItems)
        {
            InitializeTimer();
            _listSearchVideoItems = listSearchVideoItems;
            _model.MySubscribe.ResCount = _listSearchVideoItems.Count;
            _searchkey = key;
            if (!_bgv.IsBusy)
                _bgv.RunWorkerAsync("Search");
        }

        public override void GetPopularItems(string key, ObservableCollectionEx<VideoItemBase> listPopularVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void DownloadVideoInternal(IList list)
        {
            throw new NotImplementedException();
        }

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            var type = e.Argument.ToString();
            e.Result = type;

            _rtcookie = ReadCookiesFromDiskBinary(Cname);
            if (_rtcookie == null)
                AutorizeChanel();

            string zap;

            switch (type)
            {
                case "Get":

                    #region Get

                    zap = string.Format("{0}={1}", UrlUserBase, ChanelOwner);

                    MakeRtResponse(zap, ListVideoItems, false);

                    #endregion

                    break;

                case "Popular":
                    break;

                case "Search":

                    #region Search

                    zap = string.Format("{0}={1}", UrlSearchBase, _searchkey);

                    MakeRtResponse(zap, _listSearchVideoItems, true);

                    #endregion

                    break;
            }
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                TimerCommon.Dispose();
                if (e.Error is SQLiteException)
                {
                    MessageBox.Show(e.Error.Message, "Database exception", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(e.Error.Message, "Common error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (e.Result == null)
                    return;

                switch (e.Result.ToString())
                {
                    case "Get":

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
                                if (Application.Current.Dispatcher.CheckAccess())
                                {
                                    item.IsHasFile = item.IsFileExist();
                                    item.IsSynced = Sqllite.IsTableHasRecord(Subscribe.ChanelDb, item.VideoID,
                                        ChanelOwner);
                                }
                                else
                                {
                                    VideoItemBase item1 = item;
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        item1.IsHasFile = item1.IsFileExist();
                                        item1.IsSynced = Sqllite.IsTableHasRecord(Subscribe.ChanelDb, item1.VideoID,
                                            ChanelOwner);
                                    });
                                }
                            }
                            IsReady = !ListVideoItems.Select(x => x.IsSynced).Contains(false);
                            if (!IsReady)
                            {
                                var countnew = ListVideoItems.Count(x => x.IsSynced == false);
                                ChanelName = string.Format("{0} ({1})", ChanelName, countnew);
                            }
                        }

                        TimerCommon.Dispose();
                        _model.MySubscribe.Synctime = _model.MySubscribe.Synctime.Add(Synctime.Duration());

                        //Thread.Sleep(1000); //avoid too many connections

                        if (!Bgvdb.IsBusy)
                            Bgvdb.RunWorkerAsync(totalrow); //отдельный воркер для записи в базу

                        break;

                    case "Search":

                        TimerCommon.Dispose();
                        _model.MySubscribe.Result = string.Format("{0} searched in {1}", _searchkey, Synctime.Duration().ToString(@"mm\:ss"));

                        break;
                }
            }
        }

        private void MakeRtResponse(string zap, ObservableCollection<VideoItemBase> listVideoItems, bool isSearch)
        {
            listVideoItems.CollectionChanged += listVideoItems_CollectionChanged;
            HtmlDocument doc;
            var results = GetAllLinks(_rtcookie, zap, out doc);
            if (!results.Any())
            {
                AutorizeChanel();
                results = GetAllLinks(_rtcookie, zap, out doc);
            }
            foreach (HtmlNode node in results)
            {
                var v = new VideoItemRt(node)
                {
                    VideoOwner = ChanelOwner,
                    VideoOwnerName = ChanelName,
                    Num = listVideoItems.Count + 1
                };

                if (IsFull)
                {
                    if (listVideoItems.Contains(v) || string.IsNullOrEmpty(v.Title))
                        continue;
                    if (Application.Current.Dispatcher.CheckAccess())
                        listVideoItems.Add(v);
                    else
                        Application.Current.Dispatcher.Invoke(() => listVideoItems.Add(v));
                }
                else
                {
                    if (listVideoItems.Select(x => x.VideoID).Contains(v.VideoID) ||
                        string.IsNullOrEmpty(v.Title))
                        continue;
                    if (Application.Current.Dispatcher.CheckAccess())
                        listVideoItems.Insert(0, v);
                    else
                        Application.Current.Dispatcher.Invoke(() => listVideoItems.Insert(0, v));
                }
            }

            if (!IsFull)
            {
                for (int i = 0; i < listVideoItems.Count; i++)
                {
                    var k = i;
                    listVideoItems[i].Num = k + 1;
                }
                return;
            }

            var serchlinkss = isSearch ? GetAllSearchLinks(doc) : GetAllSearchLinks(doc, ChanelOwner);
            Thread.Sleep(500);
            foreach (string link in serchlinkss)
            {
                results = GetAllLinks(_rtcookie, link, out doc);
                foreach (HtmlNode nodes in results)
                {
                    var v = new VideoItemRt(nodes) {VideoOwner = ChanelOwner, VideoOwnerName = ChanelName};
                    if (!listVideoItems.Contains(v) && !string.IsNullOrEmpty(v.Title))
                    {
                        v.Num = listVideoItems.Count + 1;
                        Application.Current.Dispatcher.Invoke(() => listVideoItems.Add(v));
                    }
                }
                Thread.Sleep(500);
            }
            listVideoItems.CollectionChanged -= listVideoItems_CollectionChanged;
        }

        private static IEnumerable<string> GetAllSearchLinks(HtmlDocument doc, string pid)
        {
            var hrefTags = new List<string>();

            var counts = doc.DocumentNode.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("pg"));

            foreach (HtmlNode link in counts)
            {
                HtmlAttribute att = link.Attributes["href"];
                if (att.Value != null && !hrefTags.Contains(att.Value))
                    hrefTags.Add(att.Value);
            }

            var res = new List<string>();
            foreach (string link in hrefTags)
            {
                var raw = string.Format("http://rutracker.org/forum/{0}", link);
                var sp = raw.Split(';');
                if (sp.Length == 2)
                {
                    var raw2 = string.Format("{0}{1}&pid={2}", sp[0].Remove(sp[0].Length - 3), sp[1], pid);
                    res.Add(raw2);
                }
            }

            return res;
        }

        private static IEnumerable<string> GetAllSearchLinks(HtmlDocument doc)
        {
            var hrefTags = new List<string>();

            var counts = doc.DocumentNode.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("pg"));

            foreach (HtmlNode link in counts)
            {
                HtmlAttribute att = link.Attributes["href"];
                if (att.Value != null && !hrefTags.Contains(att.Value))
                    hrefTags.Add(att.Value);
            }

            return hrefTags.Select(link => string.Format("http://rutracker.org/forum/{0}", link)).ToList();
        }

        private static List<HtmlNode> GetAllLinks(CookieContainer cookie, string zap, out HtmlDocument doc)
        {
            var wc = new WebClientEx(cookie);
            var res = wc.DownloadString(zap);
            doc = new HtmlDocument();
            doc.LoadHtml(res);
            return doc.DocumentNode.Descendants("tr")
                .Where(
                    d =>
                        d.Attributes.Contains("class") &&
                        d.Attributes["class"].Value.Equals("tCenter hl-tr")).ToList();
        }

        private void listVideoItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollectionEx<VideoItemBase>;
            if (collection != null)
                _model.MySubscribe.ResCount = collection.Count;
        }

        private void InitializeTimer()
        {
            Synctime = new TimeSpan();
            var tcb = new TimerCallback(tmr_Tick);
            TimerCommon = new Timer(tcb, null, 0, 1000);
        }

        private void tmr_Tick(object o)
        {
            _model.MySubscribe.Result = "Working...";
            Synctime = Synctime.Add(TimeSpan.FromSeconds(1));
        }
    }
}
