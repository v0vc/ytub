using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YTub.Models;

namespace YTub.Common
{
    public class YouWrapper
    {
        private readonly bool _isffmeginpath;

        private readonly BackgroundWorker _bgv;

        #region Fields

        public string Youdl { get; set; }

        public string Ffmpeg { get; set; }

        public string SavePath { get; set; }

        public string FilePath { get; set; }

        public VideoItem Item { get; set; }

        public string VideoLink { get; set; }

        public string ClearTitle { get; set; }

        #endregion

        public YouWrapper(string youdl, string ffmpeg, string savepath, VideoItem item, bool isffmeginpath)
        {
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            Youdl = youdl;
            Ffmpeg = ffmpeg;
            SavePath = savepath;
            Item = item;
            _isffmeginpath = isffmeginpath;
            VideoLink = Item.VideoLink;
            ClearTitle = Item.ClearTitle;
        }

        public YouWrapper(string youdl, string ffmpeg, string savepath, string videolink, string cleartitle, bool isffmeginpath)
        {
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            Youdl = youdl;
            Ffmpeg = ffmpeg;
            SavePath = savepath;
            VideoLink = videolink;
            ClearTitle = cleartitle;
            _isffmeginpath = isffmeginpath;
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
            if (Item != null)
                Item.IsDownLoading = true;
            _bgv.RunWorkerAsync(isAudio);
        }

        private void DownloadFileBgv(bool isAudio)
        {
            var dir = new DirectoryInfo(SavePath);
            if (!dir.Exists)
                dir.Create();
            if (VideoLink.ToLower().Contains("youtu"))
            {
                if (_isffmeginpath)
                {
                    string param;
                    if (isAudio)
                        param = String.Format("-f bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title", SavePath, VideoLink);
                    else
                        param = String.Format("-f bestvideo+bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title", SavePath, VideoLink);

                    var startInfo = new ProcessStartInfo(Youdl, param)
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        //process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                        //process.OutputDataReceived += (sender, e) => Console.WriteLine(GetPercentFromYoudlOutput(e.Data));
                        process.OutputDataReceived += (sender, e) => SetPercentage(GetPercentFromYoudlOutput(e.Data));
                        process.BeginOutputReadLine();
                        process.Start();
                        process.WaitForExit();
                        process.Close();
                    }

                    //var proc = Process.Start(Youdl, param);
                    //if (proc != null)
                    //{
                    //    proc.WaitForExit();
                    //    proc.Close();
                    //}
                    var filename = SettingsModel.GetVersion(Youdl, String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", VideoLink));
                    FilePath = Path.Combine(SavePath, filename);
                }
                else
                {
                    //"--restrict-filenames"
                    string param;
                    if (isAudio)
                        param =String.Format("-f bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title", SavePath, VideoLink);
                    else
                        param = String.Format("-f bestvideo,bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title", SavePath, VideoLink);

                    var proc = Process.Start(Youdl, param);
                    if (proc != null)
                    {
                        proc.WaitForExit();
                        proc.Close();
                    }

                    var filename = SettingsModel.GetVersion(Youdl, String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", VideoLink));
                    var fndl = new DirectoryInfo(SavePath).GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(x => filename != null && x.Name.StartsWith(Path.GetFileNameWithoutExtension(filename))).ToList();
                    if (fndl.Count() != 2 || String.IsNullOrEmpty(Ffmpeg))
                        return;
                    //var fnvid = fndl.First(x => x.Length == fndl.Max(z => z.Length));
                    //var fnaud = fndl.First(x => x.Length == fndl.Min(z => z.Length));
                    var fnvid = fndl.First(x => Path.GetExtension(x.Name) == ".mp4");
                    var fnaud = fndl.First(x => Path.GetExtension(x.Name) == ".m4a" || Path.GetExtension(x.Name) == ".webm");
                    FilePath = MergeVideos(Ffmpeg, fnvid, fnaud, ClearTitle);
                }
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

        private void SetPercentage(double percent)
        {
            if (Item == null)
                return;
            if (Math.Abs(percent) > 0)
            {
                Application.Current.Dispatcher.BeginInvoke((Action) (() => { Item.PercentDownloaded = percent; }));
            }
            //Application.Current.Dispatcher.Invoke(() => Item.PercentDownloaded = percent);
        }

        private static double GetPercentFromYoudlOutput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;
            var regex = new Regex(@"[0-9][0-9]{0,2}\.[0-9]%", RegexOptions.None);
            var match = regex.Match(input);
            if (match.Success)
            {
                double res;
                var str = match.Value.TrimEnd('%').Replace('.', ',');
                //return Convert.ToDouble(str);
                if (double.TryParse(str, out res))
                {
                    return res;
                }
            }
            return 0;
        }
    }
}
