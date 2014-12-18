using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using YTub.Common;

namespace YTub.Video
{
    public class VideoItemRt : VideoItemBase
    {
        public int TotalDl { get; set; }

        public VideoItemRt(DbDataRecord record) : base(record)
        {
            TotalDl = (int) record["previewcount"];
        }

        public VideoItemRt(HtmlNode node)
        {
            var counts = node.Descendants("a").Where(d =>d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med tLink hl-tags bold"));
            foreach (HtmlNode htmlNode in counts)
            {
                Title = htmlNode.InnerText;
                ClearTitle = MakeValidFileName(Title);
                break;
            }

            var dl = node.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("small tr-dl dl-stub"));
            foreach (HtmlNode htmlNode in dl)
            {
                VideoLink = htmlNode.Attributes["href"].Value;
                var sp = VideoLink.Split('=');
                if (sp.Length == 2)
                    VideoID = sp[1];
                Duration = GetTorrentSize(htmlNode.InnerText);
                
                break;
            }

            var prov = node.Descendants("td").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("row4 small nowrap"));
            foreach (HtmlNode htmlNode in prov)
            {
                var pdate = htmlNode.Descendants("p");
                foreach (HtmlNode node1 in pdate)
                {
                    //var data = GetDataFromRtTorrent(node1.InnerText);
                    try
                    {
                        Published = Convert.ToDateTime(node1.InnerText);
                    }
                    catch
                    {
                    }
                    break;
                }
                break;
            }

            var seemed = node.Descendants("b").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("seedmed"));
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
        }

        public override void RunFile(object runtype)
        {
        }

        public override bool IsFileExist()
        {
            var lstnames = new List<string>
            {
                Path.Combine(Subscribe.DownloadPath, string.Format("rt-{0}({1})", VideoOwnerName, VideoOwner),
                    MakeTorrentFileName(false)),
                Path.Combine(Subscribe.DownloadPath, string.Format("rt-{0}({1})", VideoOwnerName, VideoOwner),
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

        public string MakeTorrentFileName(bool isFullName)
        {
            if (isFullName)
                return string.Format("{0}.torrent", ClearTitle);
            return string.Format("[rutracker.org].t{0}.torrent", VideoID);
        }

        private static int GetTorrentSize(string input)
        {
            double res = 0;
            var sp = input.Split(';');
            if (sp.Length == 3)
            {
                var size = sp[0].Replace("&nbsp", string.Empty).Replace('.', ',');
                if (double.TryParse(size, out res))
                {
                    var sp2 = sp[1].Split(' ');
                    if (sp2.Length == 2)
                    {
                        if (sp2[0] == "GB")
                            res = res*1000;
                        else
                            return (int) res;
                    }
                }
            }
            return (int) res;
        }
    }
}
