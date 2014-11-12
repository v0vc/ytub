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
            string savepath;
            var mpcpath = string.Empty;
            var synconstart = 0;
            var youpath = string.Empty;
            var ffpath = string.Empty;
            var fn = new FileInfo(Subscribe.ChanelDb);
            if (fn.Exists)
            {
                savepath = Sqllite.GetSettingsValue(fn.FullName, "savepath");
                mpcpath = Sqllite.GetSettingsValue(fn.FullName, "pathtompc");
                synconstart = Sqllite.GetSettingsIntValue(fn.FullName, "synconstart");
                youpath = Sqllite.GetSettingsValue(fn.FullName, "pathtoyoudl");
                ffpath = Sqllite.GetSettingsValue(fn.FullName, "pathtoffmpeg");
            }
            else
            {
                savepath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            try
            {
                var settingsModel = new SettingsModel(savepath, mpcpath, synconstart, youpath, ffpath);
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

        public void AddLink(object obj)
        {
            try
            {
                if (string.IsNullOrEmpty(Subscribe.YoudlPath))
                {
                    MessageBox.Show("Please set path to Youtube-dl in the Settings", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var addmodel = new AddLinkModel(null);
                var addlinkview = new AddLinkView
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = addmodel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                addmodel.View = addlinkview;
                addlinkview.TextBoxLink.Focus();
                addlinkview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
