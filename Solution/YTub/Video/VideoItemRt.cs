using System;
using System.Data.Common;
using System.Linq;
using HtmlAgilityPack;
using YTub.Common;

namespace YTub.Video
{
    public class VideoItemRt : VideoItemBase
    {
        public VideoItemRt(DbDataRecord record) : base(record)
        {
        }

        public VideoItemRt(HtmlNode node)
        {
            var counts = node.Descendants("a").Where(d =>d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med tLink hl-tags bold"));
            foreach (HtmlNode htmlNode in counts)
            {
                Title = htmlNode.InnerText;
                break;
            }

            var dl = node.Descendants("a").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("small tr-dl dl-stub"));
            foreach (HtmlNode htmlNode in dl)
            {
                VideoLink = htmlNode.Attributes["href"].Value;
                var sp = VideoLink.Split('=');
                if (sp.Length == 2)
                    VideoID = sp[1];
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
                Duration = Convert.ToInt32(htmlNode.InnerText);
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
            return false;
            //throw new NotImplementedException();
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
