using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YTub.Models;

namespace YTub.Common
{
    public static class YouWrapper
    {
        public static void DownloadFile(string youdl, string url, string savepath, string cleartitle, out bool isok)
        {
            isok = false;
            var dir = new DirectoryInfo(savepath);
            if (!dir.Exists)
                dir.Create();
            if (url.ToLower().Contains("youtu"))
            {
                var filename = SettingsModel.GetVersion(youdl, String.Format("--get-filename -o \"%(title)s.%(ext)s\" {0} --restrict-filenames", url));
                if (string.IsNullOrEmpty(filename)) 
                    return;
                var param = String.Format("-f bestvideo,bestaudio -o {0}\\%(title)s.%(ext)s {1} --restrict-filenames -s", savepath, url);
                var proc = Process.Start(youdl, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }

                var fndl = new DirectoryInfo(savepath).GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(x => x.Name.StartsWith(Path.GetFileNameWithoutExtension(filename))).ToList();
                if (fndl.Count() != 2 || String.IsNullOrEmpty(Subscribe.FfmpegPath)) 
                    return;
                var fnvid = fndl.First(x => x.Length == fndl.Max(z => z.Length));
                var fnaud = fndl.First(x => x.Length == fndl.Min(z => z.Length));
                isok = MergeVideos(Subscribe.FfmpegPath, fnvid, fnaud, cleartitle);
            }
            else
            {
                var param = String.Format("-o {0}\\%(title)s.%(ext)s {1}", savepath, url);
                var proc = Process.Start(youdl, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }
            }
        }



        public static bool MergeVideos(string ffmpeg, FileInfo fnvid, FileInfo fnaud, string cleartitle)
        {
            var res = false;
            var fnvidc = Path.GetFileNameWithoutExtension(fnvid.Name);
            var fnaudc = Path.GetFileNameWithoutExtension(fnaud.Name);
            if (fnvidc == fnaudc && fnvid.DirectoryName != null)
            {
                var tempname = Path.Combine(fnvid.DirectoryName, "." + fnvid.Name);
                var param = String.Format("-i \"{0}\" -i \"{1}\" -vcodec copy -acodec copy \"{2}\" -y", fnvid.FullName, fnaud.FullName, tempname);
                var proc = Process.Start(ffmpeg, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }
                var fnres = new FileInfo(tempname);
                if (fnres.Exists)
                {
                    fnvid.Delete();
                    fnaud.Delete();
                    if (string.IsNullOrEmpty(cleartitle))
                        File.Move(fnres.FullName, fnvid.FullName);
                    else
                        File.Move(fnres.FullName, cleartitle + fnvid.Extension);
                    res = true;
                }
            }

            return res;
        }

        //public static string ParseYoutubedlFilename(string inputname, out string quality)
        //{
        //    var res = String.Empty;
        //    quality = String.Empty;
        //    var sp = inputname.Split('.');
        //    if (sp[sp.Length - 2].StartsWith("f"))
        //    {
        //        //ext = sp[sp.Length - 1];
        //        quality = sp[sp.Length - 2];
        //        var sb = new StringBuilder();
        //        for (int i = 0; i < sp.Length - 2; i++)
        //        {
        //            sb.Append(sp[i]);
        //        }
        //        return sb.Append('.').Append(sp[sp.Length - 1]).ToString();
        //    }

        //    return res;
        //}

        //private static void DeleteFile(FileSystemInfo fn)
        //{
        //    //del if exist (for ffmpeg merging)
        //    if (!fn.Exists) return;
        //    try
        //    {
        //        fn.Delete();
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel.Result = ex.Message;
        //    }
        //}
    }
}
