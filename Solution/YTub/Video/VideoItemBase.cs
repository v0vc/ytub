using System;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace YTub.Video
{
    public abstract class VideoItemBase : INotifyPropertyChanged
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

        private bool _isSynced;

        private bool _isHasFile;

        private double _minProgress;

        private double _maxProgress;

        private double _percentDownloaded;

        private int _previewcount;

        private int _delta;

        private bool _isDownloading;

        #endregion

        #region Properties
        public int Num { get; set; }

        public string Title { get; set; }

        public string ClearTitle { get; set; }

        public string VideoID { get; set; }

        public string VideoOwner { get; set; }

        public string VideoOwnerName { get; set; }

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
                OnPropertyChanged();
            }
        }

        public double Duration { get; set; }

        public string VideoLink { get; set; }

        public string FilePath { get; set; }

        public string Description { get; set; }

        public string Region { get; set; }

        public string ServerName { get; set; }

        public string HostBase { get; set; }

        public DateTime Published { get; set; }

        public double MinProgress
        {
            get { return _minProgress; }
            set
            {
                _minProgress = value;
                OnPropertyChanged();
            }
        }

        public double MaxProgress
        {
            get { return _maxProgress; }
            set
            {
                _maxProgress = value;
                OnPropertyChanged();
            }
        }

        public double PercentDownloaded
        {
            get { return _percentDownloaded; }
            set
            {
                _percentDownloaded = value;
                OnPropertyChanged();
            }
        }

        public bool IsSynced
        {
            get { return _isSynced; }
            set
            {
                _isSynced = value;
                OnPropertyChanged();
            }
        }

        public bool IsHasFile
        {
            get { return _isHasFile; }
            set
            {
                _isHasFile = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownLoading
        {
            get { return _isDownloading; }
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }
        #endregion

        protected VideoItemBase(DbDataRecord record)
        {
            Title = record["title"].ToString().Replace("''", "'");
            ClearTitle = MakeValidFileName(Title);
            VideoID = record["v_id"].ToString();
            VideoOwner = record["chanelowner"].ToString();
            VideoOwnerName = record["chanelname"].ToString();
            VideoLink = record["url"].ToString();
            ServerName = record["servername"].ToString();
            ViewCount = (int) record["viewcount"];
            PrevViewCount = (int)record["previewcount"];
            Duration = (int) record["duration"];
            Description = record["description"].ToString();
            Published = (DateTime) record["published"];
        }

        protected VideoItemBase()
        {
            MinProgress = 0;
            MaxProgress = 100;
        }

        public abstract void RunFile(object runtype);

        public abstract bool IsFileExist();

        public abstract double GetTorrentSize(string input);

        public static string MakeValidFileName(string name)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var s = r.Replace(name, String.Empty);
            s = Regex.Replace(s, @"\s{2,}", " ");
            return s;
        }

        public static string AviodTooLongFileName(string path)
        {
            return path.Length > 240 ? path.Remove(240) : path;
        }

        public static void Log(string text)
        {
            if (Application.Current.Dispatcher.CheckAccess())
                ViewModelLocator.MvViewModel.Model.LogCollection.Add((text));
            else
                Application.Current.Dispatcher.BeginInvoke((Action) (() => ViewModelLocator.MvViewModel.Model.LogCollection.Add((text))));
        }

        public static string ScrubHtml(string value)
        {
            var step1 = Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
            var step2 = Regex.Replace(step1, @"\s{2,}", " ");
            return step2.Trim();
        }
    }
}
