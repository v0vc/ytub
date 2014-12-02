using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YTub.Common
{
    public class VideoItem :INotifyPropertyChanged
    {
        private bool _isSynced;

        private bool _isHasFile;

        private double _minProgress;

        private double _maxProgress;

        private double _percentDownloaded;

        private int _previewcount;

        private int _delta;

        private bool _isDownloading;

        #region Fields
        public int Num { get; set; }

        public string Title { get; set; }

        public string ClearTitle { get; set; }

        public string VideoID { get; set; }

        public string VideoOwner { get; set; }

        public int ViewCount { get; set; }

        public int PrevViewCount
        {
            get { return _previewcount; }
            set
            {
                _previewcount = value;
                Delta = ViewCount - _previewcount;
            }
        }

        public int Delta
        {
            get { return _delta; }
            set
            {
                _delta = value;
                OnPropertyChanged("Delta");
            }
        }

        public int Duration { get; set; }

        public string VideoLink { get; set; }

        public string FilePath { get; set; }

        public string Description { get; set; }

        public string Region { get; set; }

        public string ServerName { get; set; }

        public DateTime Published { get; set; }

        public double MinProgress
        {
            get { return _minProgress; }
            set
            {
                _minProgress = value;
                OnPropertyChanged("MinProgress");
            }
        }

        public double MaxProgress
        {
            get { return _maxProgress; }
            set
            {
                _maxProgress = value;
                OnPropertyChanged("MaxProgress");
            }
        }

        public double PercentDownloaded
        {
            get { return _percentDownloaded; }
            set
            {
                _percentDownloaded = value;
                OnPropertyChanged("PercentDownloaded");
            }
        }

        public bool IsSynced
        {
            get { return _isSynced; }
            set
            {
                _isSynced = value;
                OnPropertyChanged("IsSynced");
            }
        }

        public bool IsHasFile
        {
            get { return _isHasFile; }
            set
            {
                _isHasFile = value;
                OnPropertyChanged("IsHasFile");
            }
        }

        public bool IsDownLoading
        {
            get { return _isDownloading; }
            set
            {
                _isDownloading = value;
                OnPropertyChanged("IsDownLoading");
            }
        } 
        #endregion

        public VideoItem(JToken pair, bool isPopular, string region)
        {
            //MinProgress = 0;
            //MaxProgress = 100;
            try
            {
                Title = pair["title"]["$t"].ToString();
                ClearTitle = MakeValidFileName(Title);
                ViewCount = (int)pair["yt$statistics"]["viewCount"];
                Duration = (int)pair["media$group"]["yt$duration"]["seconds"];
                VideoLink = pair["link"][0]["href"].ToString().Split('&')[0];
                Published = (DateTime)pair["published"]["$t"];
                Region = region;
                var owner = pair["author"][0]["uri"]["$t"].ToString().Split('/');
                VideoOwner = owner[owner.Length - 1];
                if (!isPopular)
                {
                    var spraw = pair["id"]["$t"].ToString().Split('/');
                    VideoID = spraw[spraw.Length - 1];
                    Description = pair["content"]["$t"].ToString();
                }
                else
                {
                    var spraw = pair["id"]["$t"].ToString().Split(':');
                    VideoID = spraw[spraw.Length - 1];
                }
            }
            catch{}
        }

        public VideoItem(DbDataRecord record)
        {
            //MinProgress = 0;
            //MaxProgress = 100;
            Title = record["title"].ToString().Replace("''", "'");
            ClearTitle = MakeValidFileName(Title);
            VideoID = record["v_id"].ToString();
            VideoOwner = record["chanelowner"].ToString();
            VideoLink = record["url"].ToString();
            ViewCount = (int) record["viewcount"];
            PrevViewCount = (int)record["previewcount"];
            Duration = (int) record["duration"];
            Description = record["description"].ToString();
            Published = (DateTime) record["published"];
        }

        public VideoItem()
        {
            MinProgress = 0;
            MaxProgress = 100;
        }

        public VideoItem(HtmlNode node)
        {
            var counts = node.Descendants("a").Where(d =>d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med tLink hl-tags bold"));
            foreach (HtmlNode htmlNode in counts)
            {
                Title = htmlNode.InnerText;
                VideoLink = htmlNode.Attributes["href"].Value;     
                break;
            }

            var prov = node.Descendants("p").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("small nowrap"));
            foreach (HtmlNode htmlNode in prov)
            {
                var data = ForumItem.GetDataFromRtTorrent(htmlNode.InnerText);
                Published = Convert.ToDateTime(data);
                break;
            }

            var seemed = node.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("seedmed"));
            foreach (HtmlNode htmlNode in seemed)
            {
                ViewCount = Convert.ToInt32(htmlNode.InnerText);
                break;
            }

            var med = node.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med"));
            foreach (HtmlNode htmlNode in med)
            {
                Duration = Convert.ToInt32(htmlNode.InnerText);
                break;
            }
        }

        public bool IsFileExist(VideoItem item)
        {
            string path;
            if (!string.IsNullOrEmpty(item.VideoOwner))
                path = Path.Combine(Subscribe.DownloadPath, item.VideoOwner, string.Format("{0}.mp4", item.ClearTitle));
            else
            {
                if (!string.IsNullOrEmpty(item.ClearTitle))
                    path = Path.Combine(Subscribe.DownloadPath, string.Format("{0}.mp4", item.ClearTitle));
                else
                {
                    return false;
                }
            }

            var fn = new FileInfo(path);
            if (fn.Exists)
            {
                FilePath = path;
            }
            return fn.Exists;
        }

        public static string MakeValidFileName(string name)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var s = r.Replace(name, String.Empty);
            s = Regex.Replace(s, @"\s{2,}", " ");
            return s;
        }

        public void RunFile(object runtype)
        {
            switch (runtype.ToString())
            {
                case "Local":
                    var fn = new FileInfo(FilePath);
                    if (fn.Exists)
                    {
                        Process.Start(fn.FullName);
                    }
                    else
                    {
                        MessageBox.Show("File not exist", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                case "Online":
                    if (string.IsNullOrEmpty(Subscribe.MpcPath))
                    {
                        MessageBox.Show("Please select mpc exe file", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    var param = string.Format("{0} /play", VideoLink.Replace("https://", "http://"));
                    var proc = Process.Start(Subscribe.MpcPath, param);
                    if (proc != null) proc.Close();
                    break;
            }
        }

        //private void GetInfoAboutVideo()
        //{
        //    var wc = new System.Net.WebClient();
        //    var zap = string.Format("http://gdata.youtube.com/feeds/api/videos/{0}?alt=json&v=2", VideoID);
        //    string s = wc.DownloadString(zap);
        //    var jsvideo = (JObject)JsonConvert.DeserializeObject(s);
        //    var entry = jsvideo["entry"];
        //    Title = entry["title"]["$t"].ToString();
        //}

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
