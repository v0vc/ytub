﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YTub.Models;
using YTub.Video;

namespace YTub.Common
{
    public class YouWrapper
    {
        //private readonly bool _isffmeginpath;

        private readonly BackgroundWorker _bgv;

        private readonly List<string> _destList = new List<string>();

        private bool _isAudio;

        public delegate void MyEventHandler();

        #region Fields

        public string Youdl { get; set; }

        public string Ffmpeg { get; set; }

        public string SavePath { get; set; }

        public VideoItemBase Item { get; set; }

        public string VideoLink { get; set; }

        public string ClearTitle { get; set; }

        public event MyEventHandler Activate;

        #endregion

        public YouWrapper(string youdl, string ffmpeg, string savepath, VideoItemBase item)
        {
            _bgv = new BackgroundWorker();
            _bgv.DoWork += _bgv_DoWork;
            _bgv.RunWorkerCompleted += _bgv_RunWorkerCompleted;
            Youdl = youdl;
            Ffmpeg = ffmpeg;
            SavePath = savepath;
            Item = item;
            //_isffmeginpath = isffmeginpath;
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
            //_isffmeginpath = isffmeginpath;
        }

        private void _bgv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (Item != null && !string.IsNullOrEmpty(Item.FilePath))
                {
                    var res = "Finished " + Item.FilePath;
                    ViewModelLocator.MvViewModel.Model.LogCollection.Add(res);
                    ViewModelLocator.MvViewModel.Model.MySubscribe.Result = res;
                }
            }
            else
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "Error: " + e.Error.Message;
            }

            if (Activate != null)
                Activate.Invoke();
        }

        private void _bgv_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadFileBgv((bool) e.Argument);
        }

        public void DownloadFile(bool isAudio)
        {
            if (Item != null)
                Item.IsDownLoading = true;
            _isAudio = isAudio;
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
                string param;
                if (isAudio)
                    param =
                        String.Format(
                            "-f bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title",
                            SavePath, VideoLink);
                else
                    param =
                        String.Format(
                            "-f bestvideo,bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title --restrict-filenames",
                            SavePath, VideoLink);

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
                    process.OutputDataReceived += (sender, e) => SetLogAndPercentage(e.Data);
                    process.Start();
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    process.Close();
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

        private void processDownload_Exited()
        {
            if (_isAudio)
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "Audio OK: " + VideoLink;
                return;
            }

            var fndl = _destList.Select(s => new FileInfo(s)).Where(fn => fn.Exists).ToList();

            if (fndl.Count() != 2 || String.IsNullOrEmpty(Ffmpeg))
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.Result = "Can't merge " + VideoLink;
                return;
            }
            
            //var fnvid = fndl.First(x => x.Length == fndl.Max(z => z.Length));
            //var fnaud = fndl.First(x => x.Length == fndl.Min(z => z.Length));
            var fnvid = fndl.First(x => Path.GetExtension(x.Name) == ".mp4");
            var fnaud = fndl.First(x => Path.GetExtension(x.Name) == ".m4a"
                                        || Path.GetExtension(x.Name) == ".webm"
                                        || Path.GetExtension(x.Name) == ".aac"
                                        || Path.GetExtension(x.Name) == ".mp3");

            MergeVideos(fnvid, fnaud);
        }

        private void MergeVideos(FileInfo fnvid, FileInfo fnaud)
        {
            if (fnvid.DirectoryName == null)
                return;
            var vfolder = fnvid.DirectoryName;

            var tempname = Path.Combine(vfolder, "." + fnvid.Name);
            var param = String.Format("-i \"{0}\" -i \"{1}\" -vcodec copy -acodec copy \"{2}\" -y", fnvid.FullName,
                fnaud.FullName, tempname);

            var startInfo = new ProcessStartInfo(Ffmpeg, param)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var logg = "Merging:" + Environment.NewLine + fnvid.Name + Environment.NewLine + fnaud.Name;
            Application.Current.Dispatcher.BeginInvoke((Action) (() => ViewModelLocator.MvViewModel.Model.LogCollection.Add(logg)));

            var process = Process.Start(startInfo);

            if (process != null)
            {
                //process.EnableRaisingEvents = true;
                //process.Exited += delegate { processFfmeg_Exited(tempname, fnvid, fnaud, string.Empty); };
                process.OutputDataReceived += (sender, e) => processFfmeg_Exited(tempname, fnvid, fnaud, e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                process.Close();
            }
        }

        private void processFfmeg_Exited(string tempname, FileInfo fnvid, FileInfo fnaud, string data)
        {
            if (data != null) 
                return;
            var fnres = new FileInfo(tempname);
            if (fnres.Exists && fnres.DirectoryName != null)
            {
                FileInfo fnn;
                if (string.IsNullOrEmpty(ClearTitle))
                {
                    var filename = SettingsModel.GetVersion(Youdl,
                        String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", VideoLink));
                    fnn = new FileInfo(Path.Combine(fnres.DirectoryName, filename));
                }
                else
                {
                    fnn = new FileInfo(Path.Combine(fnres.DirectoryName, ClearTitle + Path.GetExtension(fnvid.Name)));
                }

                try
                {
                    FileHelper.RenameFile(fnres, fnn);
                    fnvid.Delete();
                    fnaud.Delete();
                    if (Item != null)
                    {
                        Item.FilePath = fnn.FullName;
                        Application.Current.Dispatcher.BeginInvoke((Action) (() => Item.IsHasFile = true));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        private void SetLogAndPercentage(string data)
        {
            if (data == null)
            {
                processDownload_Exited();
                return;
            }
            Task t = Task.Run(() =>
            {
                var dest = GetDestination(data);
                if (!string.IsNullOrEmpty(dest))
                    _destList.Add(dest);
                Application.Current.Dispatcher.BeginInvoke((Action) (() => ViewModelLocator.MvViewModel.Model.LogCollection.Add(data)));
                if (Item == null)
                    return;
                var percent = GetPercentFromYoudlOutput(data);
                if (Math.Abs(percent) > 0)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action) (() => { Item.PercentDownloaded = percent; }));
                }
            });
            t.Wait();
        }

        private static double GetPercentFromYoudlOutput(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                    return 0;
                var regex = new Regex(@"[0-9][0-9]{0,2}\.[0-9]%", RegexOptions.None);
                var match = regex.Match(input);
                if (match.Success)
                {
                    double res;
                    var str = match.Value.TrimEnd('%').Replace('.', ',');
                    if (double.TryParse(str, out res))
                    {
                        return res;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    (Action)
                        (() =>
                            ViewModelLocator.MvViewModel.Model.LogCollection.Add("GetPercentFromYoudlOutput: " +
                                                                                 ex.Message)));
                return 0;
            }
        }

        private static string GetDestination(string input)
        {
            try
            {
                var regex = new Regex(@"(\[download\] Destination: )(.+?)(\.(mp4|m4a|webm|flv|mp3))(.+)?");
                var match = regex.Match(input);
                if (match.Success)
                {
                    return regex.Replace(input, "$2$3");
                }
                regex = new Regex(@"(\[download\])(.+?)(\.(mp4|m4a|webm|flv|mp3))(.+)?");
                match = regex.Match(input);
                if (match.Success)
                {
                    return regex.Replace(input, "$2$3");
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    (Action)
                        (() =>
                            ViewModelLocator.MvViewModel.Model.LogCollection.Add("GetDestination: " + ex.Message)));
                return string.Empty;
            }
        }

        //private string MergeVideos(string ffmpeg, FileInfo fnvid, FileInfo fnaud, string cleartitle)
        //{
        //    var res = string.Empty;
        //    if (fnvid.DirectoryName == null)
        //        return res;
        //    var tempname = Path.Combine(fnvid.DirectoryName, "." + fnvid.Name);
        //    var param = String.Format("-i \"{0}\" -i \"{1}\" -vcodec copy -acodec copy \"{2}\" -y", fnvid.FullName, fnaud.FullName, tempname);

        //    var startInfo = new ProcessStartInfo(ffmpeg, param)
        //    {
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        UseShellExecute = false,
        //        RedirectStandardOutput = true,
        //        CreateNoWindow = true
        //    };

        //    var logg = "Merging:" + Environment.NewLine + fnvid.Name + Environment.NewLine + fnaud.Name;
        //    Application.Current.Dispatcher.BeginInvoke((Action)(() => ViewModelLocator.MvViewModel.Model.LogCollection.Add(logg)));

        //    var process = Process.Start(startInfo);
        //    if (process != null)
        //    {
        //        process.OutputDataReceived += (sender, e) => SetLogAndPercentage(e.Data);
        //        process.BeginOutputReadLine();
        //        process.Start();
        //        process.WaitForExit();
        //        process.Close();
        //    }

        //    //var proc = Process.Start(ffmpeg, param);
        //    //if (proc != null)
        //    //{
        //    //    proc.WaitForExit();
        //    //    proc.Close();
        //    //}

        //    var fnres = new FileInfo(tempname);
        //    if (fnres.Exists && fnres.DirectoryName != null)
        //    {
        //        fnvid.Delete();
        //        fnaud.Delete();
        //        var fnn = new FileInfo(Path.Combine(fnres.DirectoryName, fnvid.Name));
        //        File.Move(fnres.FullName, fnn.FullName);
        //        res = fnn.FullName;

        //        //if (string.IsNullOrEmpty(cleartitle))
        //        //{
        //        //    var fnn = new FileInfo(Path.Combine(fnres.DirectoryName, fnvid.Name.Replace('_', ' ')));
        //        //    File.Move(fnres.FullName, fnn.FullName);
        //        //    res = fnn.FullName;
        //        //}
        //        //else
        //        //{
        //        //    var fnn = new FileInfo(Path.Combine(fnres.DirectoryName, cleartitle + fnvid.Extension));
        //        //    File.Move(fnres.FullName, fnn.FullName);
        //        //    res = fnn.FullName;
        //        //}
        //    }
        //    return res;
        //}

        //private void DownloadFileBgv(bool isAudio, bool isffpeginpath)
        //{
        //    string param;
        //    if (isAudio)
        //        param = String.Format("-f bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title", SavePath, VideoLink);
        //    else
        //        param = String.Format("-f bestvideo+bestaudio -o {0}\\%(title)s.%(ext)s {1} --no-check-certificate --console-title", SavePath, VideoLink);

        //    var startInfo = new ProcessStartInfo(Youdl, param)
        //    {
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        UseShellExecute = false,
        //        RedirectStandardOutput = true,
        //        CreateNoWindow = true
        //    };

        //    var process = Process.Start(startInfo);
        //    if (process != null)
        //    {
        //        process.OutputDataReceived += (sender, e) => SetLogAndPercentage(e.Data);
        //        process.BeginOutputReadLine();
        //        process.Start();
        //        process.WaitForExit();
        //        //process.Close();
        //    }

        //    if (Item == null) return;
        //    var filename = SettingsModel.GetVersion(Youdl, String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", VideoLink));
        //    if (!string.IsNullOrEmpty(filename))
        //        FilePath = Path.Combine(SavePath, filename);
        //}
    }
}
