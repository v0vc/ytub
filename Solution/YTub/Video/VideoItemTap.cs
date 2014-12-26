using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using YTub.Common;

namespace YTub.Video
{
    public class VideoItemTap :VideoItemBase
    {
        public int TotalDl { get; set; }
        public VideoItemTap(DbDataRecord record) : base (record)
        {
            TotalDl = (int)record["previewcount"];
        }

        public VideoItemTap(HtmlNode node)
        {
            HostBase = "tapochek.net";
            ServerName = "Tapochek";
            var counts = node.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("genmed"));
            foreach (HtmlNode htmlNode in counts)
            {
                //Title = ScrubHtml(htmlNode.InnerText).Replace("&quot;", @"""");
                Title = HttpUtility.HtmlDecode(htmlNode.InnerText);
                ClearTitle = MakeValidFileName(Title);
                break;
            }

            var dl = node.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("small tr-dl"));
            foreach (HtmlNode htmlNode in dl)
            {
                VideoLink = string.Format("http://{0}{1}", HostBase, htmlNode.Attributes["href"].Value.TrimStart('.'));
                var sp = VideoLink.Split('=');
                if (sp.Length == 2)
                    VideoID = sp[1];
                Duration = GetTorrentSize(ScrubHtml(htmlNode.InnerText));
                
                break;
            }

            var prov = node.Descendants("td").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("row4 small nowrap"));
            foreach (HtmlNode htmlNode in prov)
            {
                var pdate = htmlNode.Descendants("p").ToList();
                if (pdate.Count == 2)
                {
                    Published = Convert.ToDateTime(pdate[1].InnerText);
                    break;
                }
            }

            var seemed = node.Descendants("td").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("row4 seedmed"));
            foreach (HtmlNode htmlNode in seemed)
            {
                ViewCount = Convert.ToInt32(htmlNode.InnerText);
                break;
            }

            var med = node.Descendants("td").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("row4 small"));
            foreach (HtmlNode htmlNode in med)
            {
                TotalDl = Convert.ToInt32(htmlNode.InnerText);
                break;
            }

            var user = node.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med"));
            foreach (HtmlNode htmlNode in user)
            {
                var uid = htmlNode.Attributes["href"].Value;
                var sp = uid.Split('=');
                if (sp.Length == 2)
                    VideoOwner = sp[1];
                VideoOwnerName = htmlNode.InnerText;
                break;
            }
        }

        public override void RunFile(object runtype)
        {
            return;
            throw new NotImplementedException();
        }

        public override bool IsFileExist()
        {
            var lstnames = new List<string>
            {
                Path.Combine(Subscribe.DownloadPath, string.Format("tap-{0}({1})", VideoOwnerName, VideoOwner),
                    MakeTorrentFileName(false)),
                Path.Combine(Subscribe.DownloadPath, string.Format("tap-{0}({1})", VideoOwnerName, VideoOwner),
                    MakeTorrentFileName(true))
            };

            foreach (string torname in lstnames)
            {
                var fn = new FileInfo(AviodTooLongFileName(torname));
                if (fn.Exists)
                {
                    FilePath = fn.FullName;
                    return fn.Exists;
                }
            }
            return false;
        }

        public override sealed double GetTorrentSize(string input)
        {
            double res = 0;
            var size = input.Trim();
            if (size.Contains("GB"))
            {
                var sizec = size.Replace("GB", string.Empty);
                if (double.TryParse(sizec, NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                {
                    return res * 1000;
                }
            }
            if (size.Contains("MB"))
            {
                var sizec = size.Replace("MB", string.Empty);
                if (double.TryParse(sizec, NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                {
                    return res;
                }
            }

            if (size.Contains("KB"))
            {
                var sizec = size.Replace("KB", string.Empty);
                if (double.TryParse(sizec, NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                {
                    return res / 1000;
                }
            }

            return res;
        }

        public string MakeTorrentFileName(bool isFullName)
        {
            if (isFullName)
                return string.Format("{0}.torrent", ClearTitle);
            return string.Format("[{0}].t{1}.torrent", HostBase, VideoID);
        }

    }
}
