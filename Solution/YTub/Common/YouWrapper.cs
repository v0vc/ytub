using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YTub.Models;

namespace YTub.Common
{
    public static class YouWrapper
    {
        public static void DownloadFile(string youdl, string url, string savepath)
        {
            if (url.ToLower().Contains("youtu"))
            {
                var filename = SettingsModel.GetVersion(youdl, string.Format("--get-filename -o \"%(title)s.%(ext)s\" {0}", url));
                var clearname = VideoItem.MakeValidFileName(filename);
                var fn = new FileInfo(Path.Combine(savepath, clearname));
                if (fn.Exists)
                {
                    try
                    {
                        fn.Delete();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                var param = string.Format("-f bestvideo+bestaudio -o {0}\\%(title)s.%(ext)s {1}", savepath, url);
                var proc = System.Diagnostics.Process.Start(youdl, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }

                var fndl = new DirectoryInfo(savepath).GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(x => x.Name.StartsWith(Path.GetFileNameWithoutExtension(fn.Name))).ToList();
                if (fndl.Count() != 2) return;
                if (!string.IsNullOrEmpty(Subscribe.FfmpegPath))
                {
                    var fnvid = fndl.First(x => x.Length == fndl.Max(z => z.Length));
                    var fnaud = fndl.First(x => x.Length == fndl.Min(z => z.Length));
                    bool isok;
                    Chanel.MergeVideos(Subscribe.FfmpegPath, fnvid, fnaud, out isok);
                }
                else
                {
                    foreach (FileInfo fileInfo in fndl)
                    {
                        string quality;
                        var fname = Chanel.ParseYoutubedlFilename(fileInfo.Name, out quality);
                        if (!string.IsNullOrEmpty(quality) && fileInfo.DirectoryName != null)
                            File.Move(fileInfo.FullName, Path.Combine(fileInfo.DirectoryName, fname));
                    }
                }
            }
            else
            {
                var param = string.Format("-o {0}\\%(title)s.%(ext)s {1}", savepath, url);
                var proc = System.Diagnostics.Process.Start(youdl, param);
                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Close();
                }
            }
        }
    }
}
