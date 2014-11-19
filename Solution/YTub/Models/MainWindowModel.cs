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
using System.Xml.Linq;
using Microsoft.Win32;
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
            var isonlyfavor = 0;
            var youpath = string.Empty;
            var ffpath = string.Empty;
            var fn = new FileInfo(Subscribe.ChanelDb);
            if (fn.Exists)
            {
                savepath = Sqllite.GetSettingsValue(fn.FullName, "savepath");
                mpcpath = Sqllite.GetSettingsValue(fn.FullName, "pathtompc");
                synconstart = Sqllite.GetSettingsIntValue(fn.FullName, "synconstart");
                isonlyfavor = Sqllite.GetSettingsIntValue(fn.FullName, "isonlyfavor");
                youpath = Sqllite.GetSettingsValue(fn.FullName, "pathtoyoudl");
                ffpath = Sqllite.GetSettingsValue(fn.FullName, "pathtoffmpeg");
            }
            else
            {
                savepath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            try
            {
                var settingsModel = new SettingsModel(savepath, mpcpath, synconstart, youpath, ffpath, isonlyfavor);
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

        public void BackupRestore(object obj)
        {
            switch (obj.ToString())
            {
                case "backup":
                    Backup();
                    break;

                case "restore":
                    Restore();
                    break;
            }
        }

        private static void Backup()
        {
            var dlg = new SaveFileDialog
            {
                FileName = "backup_" + DateTime.Now.ToShortDateString(),
                DefaultExt = ".xml",
                Filter = "XML documents (.xml)|*.xml",
                OverwritePrompt = true
            };
            var res = dlg.ShowDialog();
            if (res == true)
            {
                var doc = new XDocument(new XElement("tables", new XElement("tblSettings",
                    new XElement("savepath", Sqllite.GetSettingsValue(Subscribe.ChanelDb, "savepath")),
                    new XElement("pathtompc", Sqllite.GetSettingsValue(Subscribe.ChanelDb, "pathtompc")),
                    new XElement("synconstart", Sqllite.GetSettingsIntValue(Subscribe.ChanelDb, "synconstart")),
                    new XElement("isonlyfavor", Sqllite.GetSettingsIntValue(Subscribe.ChanelDb, "isonlyfavor")),
                    new XElement("pathtoyoudl", Sqllite.GetSettingsValue(Subscribe.ChanelDb, "pathtoyoudl")),
                    new XElement("pathtoffmpeg", Sqllite.GetSettingsValue(Subscribe.ChanelDb, "pathtoffmpeg"))
                    ), new XElement("tblVideos")));

                var element = doc.Element("tables");
                if (element != null)
                {
                    var xElement = element.Element("tblVideos");
                    if (xElement != null)
                    {
                        foreach (KeyValuePair<string, string> pair in Sqllite.GetDistinctValues(Subscribe.ChanelDb, "chanelowner", "chanelname"))
                        {
                            xElement.Add(new XElement("Chanell", 
                                new XElement("chanelowner", pair.Key), new XElement("chanelname", pair.Value)));
                        }
                    }
                }
                doc.Save(dlg.FileName);
            }
        }

        private static void Restore()
        {
            var opf = new OpenFileDialog {Filter = "XML documents (.xml)|*.xml"};
            var res = opf.ShowDialog();
            if (res == true)
            {
                try
                {
                    var doc = XDocument.Load(opf.FileName);
                    var dicv = doc.Descendants("tblSettings").Elements().ToDictionary(setting => setting.Name.LocalName, setting => setting.Value);
                    var dic = new Dictionary<string, string>();
                    foreach (XElement element in doc.Descendants("tblSettings").Elements())
                    {
                        if (element.Name.LocalName == "synconstart" || element.Name.LocalName == "isonlyfavor")
                            dic.Add(element.Name.LocalName, "INT");
                        else
                            dic.Add(element.Name.LocalName, "TEXT");
                    }
                    Sqllite.DropTable(Subscribe.ChanelDb, "tblSettings");
                    Sqllite.CreateTable(Subscribe.ChanelDb, "tblSettings", dic);
                    Sqllite.CreateSettings(Subscribe.ChanelDb, "tblSettings", dicv);

                    var xElement1 = doc.Element("tables");
                    if (xElement1 == null) return;
                    var xElement = xElement1.Element("tblVideos");
                    if (xElement == null) return;
                    foreach (XElement element in xElement.Descendants("Chanell"))
                    {
                        var owner = element.Elements().FirstOrDefault(z => z.Name == "chanelowner");
                        var name = element.Elements().FirstOrDefault(z => z.Name == "chanelname");
                        if (owner != null & name != null)
                        {
                            ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Add(new Chanel(name.Value, owner.Value));
                            ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites = false;
                        }
                    }

                    Subscribe.DownloadPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, "savepath");
                    Subscribe.MpcPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, "pathtompc");
                    Subscribe.YoudlPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, "pathtoyoudl");
                    Subscribe.FfmpegPath = Sqllite.GetSettingsValue(Subscribe.ChanelDb, "pathtoffmpeg");
                }
                catch (Exception ex)
                {
                    ViewModelLocator.MvViewModel.Model.MySubscribe.Result = ex.Message;
                }
            }
        }
    }
}
