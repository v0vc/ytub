using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using YTub.Common;
using YTub.Video;

namespace YTub.Chanell
{
    public class ChanelRt :ChanelBase
    {
        public ChanelRt(string chaneltype, string login, string pass, string chanelname, string chanelowner, int ordernum) : base(chaneltype, login, pass, chanelname, chanelowner, ordernum)
        {

        }

        public override CookieContainer GetSession()
        {
            var cc = new CookieContainer();
            cc.Add(new Cookie("tr_simple", "1", "", "rutracker.org"));
            var req = (HttpWebRequest)WebRequest.Create("http://login.rutracker.org/forum/login.php");
            req.CookieContainer = cc;
            req.Method = "POST";
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

        public override void GetItemsFromNet()
        {
            //var rtcookie = ChanelForum.GetSessionRt();
            //if (rtcookie.Count > 0)
            //    ForumItem.WriteCookiesToDiskBinary(rtcookie);

            var rtcookie = ReadCookiesFromDiskBinary();
            var wc = new WebClientEx(rtcookie);
            var zap = string.Format("http://rutracker.org/forum/tracker.php?rid={0}", ChanelOwner);
            string s = wc.DownloadString(zap);
            var doc = new HtmlDocument();
            doc.LoadHtml(s);
            var results = doc.DocumentNode.Descendants("tr").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("tCenter hl-tr"));
            foreach (HtmlNode node in results)
            {
                var v = new VideoItemRt(node);
                if (!ListVideoItems.Contains(v) && !string.IsNullOrEmpty(v.Title))
                {
                    v.Num = ListVideoItems.Count + 1;
                    Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
                }
            }

            //var serchlinks = GetAllSearchLinks(doc);
            //foreach (string link in serchlinks)
            //{
            //    Debug.WriteLine(link);
            //}

            //var v = new VideoItem {Title = "RuTracker not implemented yet"};
            //Application.Current.Dispatcher.Invoke(() => ListVideoItems.Add(v));
        }

        private IEnumerable<string> GetAllSearchLinks(HtmlDocument doc)
        {
            var hrefTags = new List<string>();

            var counts = doc.DocumentNode.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("pg"));

            foreach (HtmlNode link in counts)
            {
                HtmlAttribute att = link.Attributes["href"];
                if (att.Value != null && !hrefTags.Contains(att.Value))
                    hrefTags.Add(att.Value);
            }

            return hrefTags;
        }
    }
}
