using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YTub.Models;

namespace YTub.Common
{
    public class YouWrapper
    {
        public string Youdl { get; set; }

        public string Ffmpeg { get; set; }

        public string SavePath { get; set; }

        public string FilePath { get; set; }

        public VideoItem Item { get; set; }

        public string VideoLink { get; set; }

        public string ClearTitle { get; set; }

        private readonly BackgroundWorker _bgv;

        public YouWrapper(string youdl, string ffmpeg, string savepath, VideoItem item)
        {
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            Youdl = youdl;
            Ffmpeg = ffmpeg;
            SavePath = savepath;
            Item = item;
            VideoLink = Item.VideoLink;
            ClearTitle = Item.ClearTitle;
        }

        public YouWrapper(string youdl, string ffmpeg, string savepath, string videolink, string cleartitle)
        {
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            Youdl = youdl;
            Ffmpeg = ffmpeg;
            SavePath = savepath;
            VideoLink = videolink;
            ClearTitle = cleartitle;
        }

        void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FilePath) && Item != null)
            {
                Item.FilePath = FilePath;
                Item.IsHasFile = true;
            }
        }

        void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadFileBgv((bool) e.Argument);
        }

        public void DownloadFile(bool isAudio)
        {
            _bgv.RunWorkerAsync(isAudio);
        }

        private void DownloadFileBgv(bool isAudio)
        {
            var dir = new DirectoryInfo(SavePath);
            if (!dir.Exists)
                dir.Create();
            if (VideoLink.ToLower().Contains("youtu"))
            {
                //"--restrict-filenames"
                var filename = SettingsModel.GetVersion(Youdl, String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", VideoLink));
                string param;
                if (isAudio)
                    param =
                        String.Format(
                            "-f bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title",
                            SavePath, VideoLink);
                else
                {
                    param =
                        String.Format(
                            "-f bestvideo,bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title",
                            SavePath, VideoLink);
                }

                var proc = Process.Start(Youdl, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }

                var fndl = new DirectoryInfo(SavePath).GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(x => filename != null && x.Name.StartsWith(Path.GetFileNameWithoutExtension(filename))).ToList();
                if (fndl.Count() != 2 || String.IsNullOrEmpty(Ffmpeg))
                    return;
                //var fnvid = fndl.First(x => x.Length == fndl.Max(z => z.Length));
                //var fnaud = fndl.First(x => x.Length == fndl.Min(z => z.Length));
                var fnvid = fndl.First(x => Path.GetExtension(x.Name) == ".mp4");
                var fnaud = fndl.First(x => Path.GetExtension(x.Name) == ".m4a");
                FilePath = MergeVideos(Ffmpeg, fnvid, fnaud, ClearTitle);
            }
            else
            {
                var param = String.Format("-o {0}\\%(title)s.%(ext)s {1} --no-check-certificate", SavePath, VideoLink);
                var proc = Process.Start(Youdl, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }
            }
        }

        private static string MergeVideos(string ffmpeg, FileInfo fnvid, FileInfo fnaud, string cleartitle)
        {
            var res = string.Empty;
            if (fnvid.DirectoryName == null)
                return res;
            var tempname = Path.Combine(fnvid.DirectoryName, "." + fnvid.Name);
            var param = String.Format("-i \"{0}\" -i \"{1}\" -vcodec copy -acodec copy \"{2}\" -y", fnvid.FullName, fnaud.FullName, tempname);
            var proc = Process.Start(ffmpeg, param);
            if (proc != null)
            {
                proc.WaitForExit();
                proc.Close();
            }
            var fnres = new FileInfo(tempname);
            if (fnres.Exists && fnres.DirectoryName != null)
            {
                fnvid.Delete();
                fnaud.Delete();
                if (string.IsNullOrEmpty(cleartitle))
                {
                    var fnn = new FileInfo(Path.Combine(fnres.DirectoryName, fnvid.Name.Replace('_', ' ')));
                    File.Move(fnres.FullName, fnn.FullName);
                    res = fnn.FullName;
                }
                else
                {
                    var fnn = new FileInfo(Path.Combine(fnres.DirectoryName, cleartitle + fnvid.Extension));
                    File.Move(fnres.FullName, fnn.FullName);
                    res = fnn.FullName;
                }
            }
            return res;
        }
    }
}
