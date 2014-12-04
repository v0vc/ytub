using System;
using System.Linq;
using HtmlAgilityPack;
using YTub.Common;

namespace YTub.Video
{
    public class VideoItemRt : VideoItemBase
    {
        public VideoItemRt(HtmlNode node)
        {
            var counts = node.Descendants("a").Where(d =>d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med tLink hl-tags bold"));
            foreach (HtmlNode htmlNode in counts)
            {
                Title = htmlNode.InnerText;
                VideoLink = htmlNode.Attributes["href"].Value;     
                break;
            }

            var prov = node.Descendants("p").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("small nowrap"));
            foreach (HtmlNode htmlNode in prov)
            {
                var data = GetDataFromRtTorrent(htmlNode.InnerText);
                Published = Convert.ToDateTime(data);
                break;
            }

            var seemed = node.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("seedmed"));
            foreach (HtmlNode htmlNode in seemed)
            {
                ViewCount = Convert.ToInt32(htmlNode.InnerText);
                break;
            }

            var med = node.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Equals("med"));
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
