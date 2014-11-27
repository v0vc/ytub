using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace YTub.Common
{
    public class FileHelper
    {
        public static string GetFileProcessName(string filePath)
        {
            Process[] procs = Process.GetProcesses();
            //string fileName = Path.GetFileName(filePath);

            foreach (Process proc in procs)
            {
                if (proc.MainWindowHandle != new IntPtr(0) && !proc.HasExited)
                {
                    //if (proc.Modules.Cast<ProcessModule>().Any(pm => pm.ModuleName == fileName))
                    {
                        return proc.ProcessName;
                    }
                }
            }

            return null;
        }

        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }

        public static bool RenameFile(FileInfo oldFile, FileInfo newFile, out string res)
        {
            res = string.Empty;
            if (oldFile.Exists)
            {
                if (!IsFileLocked(newFile))
                {
                    File.Move(oldFile.FullName, newFile.FullName);    
                    return true;
                }
                res = GetFileProcessName(newFile.FullName);
                return false;
            }
            return false;
        }

        public static void RenameFile(FileInfo oldFile, FileInfo newFile)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    File.Move(oldFile.FullName, newFile.FullName);
                    break;
                }
                catch
                {
                    Thread.Sleep(10);
                    i++;
                }
            }
        }
    }
}
