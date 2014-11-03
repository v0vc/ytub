using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YTub.Common
{
    public class VideoItem :INotifyPropertyChanged
    {
        private bool _isSynced;

        public int Num { get; set; }

        public string Title { get; set; }

        public string VideoID { get; set; }

        public int ViewCount { get; set; }

        public int Duration { get; set; }

        public string VideoLink { get; set; }

        //public DateTime Updated { get; set; }

        public string Description { get; set; }

        public DateTime Published { get; set; }

        public bool IsSynced
        {
            get { return _isSynced; }
            set
            {
                _isSynced = value;
                OnPropertyChanged("IsSynced");
            }
        }

        public VideoItem(JToken pair)
        {
            Title = pair["title"]["$t"].ToString();
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
            Title = record["title"].ToString();
            VideoID = record["v_id"].ToString();
            VideoLink = record["url"].ToString();
            ViewCount = (int) record["viewcount"];
            Duration = (int) record["duration"];
            Description = record["description"].ToString();
            Published = (DateTime) record["published"];
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
