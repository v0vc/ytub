using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YTub.Common;
using YTub.Views;

namespace YTub.Models
{
    public class MainWindowModel 
    {
        public Subscribe MySubscribe { get; set; }

        public MainWindowModel()
        {
            MySubscribe = new Subscribe();
        }

        public void OpenSettings(object obj)
        {
            string path;
            var fn = new FileInfo(Subscribe.ChanelDb);
            if (fn.Exists)
            {
                path = Sqllite.GetDownloadPath(fn.FullName);
            }
            else
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            try
            {
                var settingsModel = new SettingsModel(path);
                var settingslView = new SettingsView
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = settingsModel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                settingslView.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
