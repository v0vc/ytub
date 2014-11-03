using System.Windows;
using System.Windows.Input;

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
            ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel(null);
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
    }
}
