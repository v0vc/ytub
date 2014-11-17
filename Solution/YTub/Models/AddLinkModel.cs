using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YTub.Common;
using YTub.Views;

namespace YTub.Models
{
    public class AddLinkModel :INotifyPropertyChanged
    {
        private string _link;

        private bool _isAudio;

        public AddLinkView View { get; set; }

        public string Link
        {
            get { return _link; }
            set
            {
                _link = value; 
                OnPropertyChanged("Link");
            }
        }

        public RelayCommand GoCommand { get; set; }

        public bool IsAudio
        {
            get { return _isAudio; }
            set
            {
                _isAudio = value;
                OnPropertyChanged("IsAudio");
            }
        }

        public AddLinkModel(AddLinkView view)
        {
            View = view;
            GoCommand = new RelayCommand(x => Go());
            try
            {
                var text = Clipboard.GetData(DataFormats.Text) as string;
                if (string.IsNullOrWhiteSpace(text) || text.Contains(Environment.NewLine))
                    return;
                Link = text;
            }
            catch (Exception ex)
            {
                Link = ex.Message;
            }
        }

        private void Go()
        {
            if (IsValidUrl(Link))
            {
                View.Close();
                var youdl = new YouWrapper(Subscribe.YoudlPath, Subscribe.FfmpegPath, Subscribe.DownloadPath, Link, null);
                youdl.DownloadFile(IsAudio);
            }
            else
            {
                Link = "Not valid URL";
            }
        }

        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) || null == uri)
            {
                return false;
            }
            return true;
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
