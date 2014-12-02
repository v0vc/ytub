using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace YTub.Common
{
    public class ForumItem :INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private const string Cname = "rtcookie.ck";

        private string _password;

        private string _login;

        public string ForumName { get; set; }

        public string Login
        {
            get { return _login; }
            set
            {
                _login = value;
                OnPropertyChanged("Login");
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }

        public ForumItem(string forumname, string login, string pass)
        {
            ForumName = forumname;
            Login = login;
            Password = pass;
        }

        public CookieContainer GetSessionRt()
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

        public CookieContainer GetSessionTap()
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

            var resp = (HttpWebResponse) req.GetResponse();
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                cc.Add(resp.Cookies);
            }

            return cc;
        }

        public static void WriteCookiesToDiskBinary(CookieContainer cookieJar)
        {
            var fn = new FileInfo(Path.Combine(Sqllite.AppDir, Cname));
            if (fn.Exists)
            {
                try
                {
                    fn.Delete();
                }
                catch (Exception e)
                {
                    ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "WriteCookiesToDiskBinary: " + e.Message;
                }
            }
            using (Stream stream = File.Create(fn.FullName))
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                }
                catch (Exception e)
                {
                    ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "WriteCookiesToDiskBinary: " + e.Message;
                }
            }
        }

        public static CookieContainer ReadCookiesFromDiskBinary()
        {
            try
            {
                var fn = new FileInfo(Path.Combine(Sqllite.AppDir, Cname));

                using (Stream stream = File.Open(fn.FullName, FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "ReadCookiesFromDiskBinary: " + e.Message;
            }
            return null;
        }

        public static string GetDataFromRtTorrent(string input)
        {
            var sp = input.Split(':');
            if (sp.Length == 3)
            {
                var sp1 = sp[1].Split(';');
                if (sp1.Length == 4)
                {
                    return sp1[1].Replace("&nbsp", string.Empty);
                }
            }
            return string.Empty;
        }
    }
}
