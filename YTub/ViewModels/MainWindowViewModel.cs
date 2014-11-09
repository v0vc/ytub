using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YTub.Common;
using YTub.Models;
using YTub.Views;

namespace YTub.ViewModels
{
    public class MainWindowViewModel
    {
        public MainWindowModel Model { get; set; }

        public RelayCommand OpenAddChanelCommand { get; set; }

        public RelayCommand SelectChanelCommand { get; set; }

        public RelayCommand OpenSettingsCommand { get; set; }

        public RelayCommand RemoveChanelCommand { get; set; }

        public RelayCommand PlayFileCommand { get; set; }

        public MainWindowViewModel(MainWindowModel model)
        {
            Model = model;
            OpenAddChanelCommand = new RelayCommand(Model.MySubscribe.AddChanel);
            SelectChanelCommand = new RelayCommand(Model.MySubscribe.SyncChanel);
            RemoveChanelCommand = new RelayCommand(Model.MySubscribe.RemoveChanel);
            OpenSettingsCommand = new RelayCommand(Model.OpenSettings);
            PlayFileCommand = new RelayCommand(Model.MySubscribe.PlayFile);
        }
    }
}
