using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YoutubeExtractor;
using YTub.Common;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace YTub.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RemoveChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.RemoveChanel(null);
        }

        private void SyncChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel("SyncChanelSelected");
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.GetChanelsFromDb();
        }

        private void EditChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanel("edit");
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Insert)
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanel(null);
            }
        }

        private void DownloadVideoOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel.DownloadVideo();
        }

        private void AddToQueueOnClick(object sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void PlayOnClick(object sender, RoutedEventArgs e)
        {
            var sndr = sender as MenuItem;
            if (sndr == null)
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.PlayFile("Online");
            }
            else
            {
                switch (sndr.CommandParameter.ToString())
                {
                    case "Local":
                        ViewModelLocator.MvViewModel.Model.MySubscribe.PlayFile(sndr.CommandParameter.ToString());
                        break;

                    case "Online":
                        ViewModelLocator.MvViewModel.Model.MySubscribe.PlayFile(sndr.CommandParameter.ToString());
                        break;
                }    
            }
        }

        private void PlayLocalButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.PlayFile("Local");
        }
    }
}
