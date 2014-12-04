using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Newtonsoft.Json.Linq;
using YTub.Common;

namespace YTub.Video
{
    class VideoItemYou : VideoItemBase
    {
        public VideoItemYou(JToken pair, bool isPopular, string region)
        {
            try
            {
                Title = pair["title"]["$t"].ToString();
                ClearTitle = MakeValidFileName(Title);
                ViewCount = (int) pair["yt$statistics"]["viewCount"];
                Duration = (int) pair["media$group"]["yt$duration"]["seconds"];
                VideoLink = pair["link"][0]["href"].ToString().Split('&')[0];
                Published = (DateTime) pair["published"]["$t"];
                Region = region;
                var owner = pair["author"][0]["uri"]["$t"].ToString().Split('/');
                VideoOwner = owner[owner.Length - 1];
                if (!isPopular)
                {
                    var spraw = pair["id"]["$t"].ToString().Split('/');
                    VideoID = spraw[spraw.Length - 1];
                    Description = pair["content"]["$t"].ToString();
                }
                else
                {
                    var spraw = pair["id"]["$t"].ToString().Split(':');
                    VideoID = spraw[spraw.Length - 1];
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        public VideoItemYou(DbDataRecord record) : base(record)
        {
        }

        public override void RunFile(object runtype)
        {
            switch (runtype.ToString())
            {
                case "Local":
                    var fn = new FileInfo(FilePath);
                    if (fn.Exists)
                    {
                        Process.Start(fn.FullName);
                    }
                    else
                    {
                        MessageBox.Show("File not exist", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                case "Online":
                    if (string.IsNullOrEmpty(Subscribe.MpcPath))
                    {
                        MessageBox.Show("Please select mpc exe file", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    var param = string.Format("{0} /play", VideoLink.Replace("https://", "http://"));
                    var proc = Process.Start(Subscribe.MpcPath, param);
                    if (proc != null) proc.Close();
                    break;
            }
        }

        public override bool IsFileExist()
        {
            string path;
            if (!string.IsNullOrEmpty(VideoOwner))
                path = Path.Combine(Subscribe.DownloadPath, VideoOwner, string.Format("{0}.mp4", ClearTitle));
            else
            {
                if (!string.IsNullOrEmpty(ClearTitle))
                    path = Path.Combine(Subscribe.DownloadPath, string.Format("{0}.mp4", ClearTitle));
                else
                {
                    return false;
                }
            }

            var fn = new FileInfo(path);
            if (fn.Exists)
            {
                FilePath = path;
            }
            return fn.Exists;
        }
    }
}
