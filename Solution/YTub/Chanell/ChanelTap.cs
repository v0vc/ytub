using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YTub.Common;
using YTub.Video;

namespace YTub.Chanell
{
    public class ChanelTap : ChanelBase
    {
        public ChanelTap(string chaneltype, string login, string pass, string chanelname, string chanelowner, int ordernum) : base(chaneltype, login, pass, chanelname, chanelowner, ordernum)
        {
        }

        public override CookieContainer GetSession()
        {
            var cc = new CookieContainer();
            var req = (HttpWebRequest)WebRequest.Create("http://tapochek.net/login.php");
            req.Method = "POST";
            req.Host = "login.rutracker.org";
            req.KeepAlive = true;
            var postData = string.Format("login_username={0}&login_password={1}&login=%C2%F5%EE%E4",
                Uri.EscapeDataString(Login), Uri.EscapeDataString(Password));
            var data = Encoding.ASCII.GetBytes(postData);
            req.ContentLength = data.Length;
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
            req.ContentType = "application/x-www-form-urlencoded";
            req.Headers.Add("Cache-Control", "max-age=0");
            req.Headers.Add("Origin", @"http://tapochek.net");
            req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
            req.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            req.Headers.Add("DNT", "1");
            req.Referer = @"http://tapochek.net/index.php";

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

        public override void GetItemsFromNet()
        {
            return;
            throw new NotImplementedException();
        }

        public override void AutorizeChanel()
        {
            return;
        }

        public override void DownloadItem()
        {
            throw new NotImplementedException();
        }

        public override void SearchItems(string key, TrulyObservableCollection<VideoItemBase> listSearchVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void GetPopularItems(string key, TrulyObservableCollection<VideoItemBase> listPopularVideoItems)
        {
            throw new NotImplementedException();
        }
    }
}
