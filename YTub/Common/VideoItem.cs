using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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

        private bool _isDownloading;
        
        public int Num { get; set; }

        public string Title { get; set; }

        public string ClearTitle { get; set; }

        public string VideoID { get; set; }

        public int ViewCount { get; set; }

        public int Duration { get; set; }

        public string VideoLink { get; set; }

        public string FilePath { get; set; }

        //public DateTime Updated { get; set; }

        public string Description { get; set; }

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

        public VideoItem(JToken pair)
        {
            MinProgress = 0;
            MaxProgress = 100;
            Title = pair["title"]["$t"].ToString();
            ClearTitle = MakeValidFileName(Title);
            var spraw = pair["id"]["$t"].ToString().Split('/');
            VideoID = spraw[spraw.Length - 1];
            ViewCount = (int)pair["yt$statistics"]["viewCount"];
            Duration = (int) pair["media$group"]["yt$duration"]["seconds"];
            VideoLink = pair["link"][0]["href"].ToString().Split('&')[0];
            //Updated = (DateTime) pair["updated"]["$t"];
            Published = (DateTime)pair["published"]["$t"];
            Description = pair["content"]["$t"].ToString();
        }

        public VideoItem(DbDataRecord record)
        {
            MinProgress = 0;
            MaxProgress = 100;
            Title = record["title"].ToString().Replace("''", "'");
            ClearTitle = MakeValidFileName(Title);
            VideoID = record["v_id"].ToString();
            VideoLink = record["url"].ToString();
            ViewCount = (int) record["viewcount"];
            Duration = (int) record["duration"];
            Description = record["description"].ToString();
            Published = (DateTime) record["published"];
        }

        public bool IsFileExist(string title)
        {
            var path = Path.Combine(Subscribe.DownloadPath, string.Format("{0}.mp4", title));
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
            return r.Replace(name, String.Empty);
            //return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
        }

        public void RunFile(object runtype)
        {
            switch (runtype.ToString())
            {
                case "Local":
                    var fn = new FileInfo(FilePath);
                    if (fn.Exists)
                    {
                        System.Diagnostics.Process.Start(fn.FullName);
                    }
                    else
                    {
                        MessageBox.Show("File not exist", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                case "Online":
                    var param = string.Format("{0} /play", VideoLink.Replace("https://", "http://"));
                    var proc = System.Diagnostics.Process.Start(Subscribe.MpcPath, param);
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
