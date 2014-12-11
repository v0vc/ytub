using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YoutubeExtractor;
using YTub.Chanell;
using YTub.Common;
using YTub.Video;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace YTub.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty DraggedItemProperty = DependencyProperty.Register("DraggedItem", typeof(ChanelBase), typeof(Window));
        public bool IsDragging { get; set; }

        public bool IsEditing { get; set; }

        public ChanelBase DraggedItem
        {
            get { return (ChanelBase)GetValue(DraggedItemProperty); }
            set { SetValue(DraggedItemProperty, value); }
        }

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

        private void AutorizeChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel.AutorizeChanel();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModelLocator.MvViewModel.Model.MySubscribe.GetChanelsFromDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message);
            }
            //DataGridChanels.UnselectAll();
        }

        private void EditChanelOnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanel("edit");
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Insert)
            {
                //ViewModelLocator.MvViewModel.Model.MySubscribe.AddChanel(null);
                ViewModelLocator.MvViewModel.Model.AddLink(null);
            }
        }

        private void DownloadVideoOnClick(object sender, RoutedEventArgs e)
        {
            var sndr = sender as MenuItem;
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel is ChanelYou)
            {
                var c = cchanel as ChanelYou;
                if (sndr == null)
                {
                    c.DownloadVideoInternal();
                }
                else
                {
                    c.DownloadItem();
                }
            }
        }

        private void PlayOnClick(object sender, RoutedEventArgs e)
        {
            var sndr = sender as MenuItem;
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null)
            {
                if (sndr == null)
                {
                    if (cchanel.CurrentVideoItem.IsHasFile)
                        cchanel.CurrentVideoItem.RunFile("Local");
                    else
                        cchanel.CurrentVideoItem.RunFile("Online");
                }
                else
                {
                    cchanel.CurrentVideoItem.RunFile(sndr.CommandParameter.ToString());
                }
            }
        }

        private void PlayLocalButton_OnClick(object sender, RoutedEventArgs e)
        {
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null)
            {
                if (cchanel.CurrentVideoItem.IsHasFile)
                    cchanel.CurrentVideoItem.RunFile("Local");
                else
                {
                    if (cchanel is ChanelYou)
                    {
                        (cchanel as ChanelYou).DownloadItem();
                    }
                    if (cchanel is ChanelRt)
                    {
                        (cchanel as ChanelRt).DownloadItem();
                    }
                }
            }
        }

        private void CopyLinkOnClick(object sender, RoutedEventArgs e)
        {
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null && cchanel.CurrentVideoItem != null)
            {
                try
                {
                    Clipboard.SetText(cchanel.CurrentVideoItem.VideoLink);
                }
                catch{}
            }
        }

        private void DeleteOnClick(object sender, RoutedEventArgs e)
        {
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null && cchanel.CurrentVideoItem != null)
            {
                cchanel.DeleteFiles();
            }
        }

        private void Favour_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null)
            {
                cchanel.AddToFavorites();
            }
        }

        private void Row_doubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.SyncChanel("SyncChanelSelected");
        }

        private void ButtonShowHideFavor_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites = !ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites;
        }

        private void CopyAuthorOnClick(object sender, RoutedEventArgs e)
        {
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;
            if (cchanel != null && cchanel.CurrentVideoItem != null)
            {
                try
                {
                    Clipboard.SetText(cchanel.CurrentVideoItem.VideoOwner);
                }
                catch{}
            }
        }

        private void OnMouseLeftButtonUpChanells(object sender, MouseButtonEventArgs e)
        {
            if (!IsDragging || IsEditing)
            {
                return;
            }

            //get the target item
            var cchanel = ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel;

            if (cchanel == null || !ReferenceEquals(DraggedItem, cchanel))
            {
                var draggedIndex = ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.IndexOf(DraggedItem);

                //remove the source from the list
                ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.Remove(DraggedItem);

                //get target index
                var targetIndex = ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.IndexOf(cchanel);

                if (targetIndex < draggedIndex)
                {
                    //move source at the target's location
                    ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.Insert(targetIndex, DraggedItem);
                }

                if (targetIndex >= draggedIndex)
                {
                    //move source at the target's location
                    ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.Insert(targetIndex + 1, DraggedItem);
                }

                //update db
                draggedIndex = ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.IndexOf(DraggedItem);
                targetIndex = ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.IndexOf(cchanel);
                Sqllite.UpdateChanelOrder(Subscribe.ChanelDb, DraggedItem.ChanelOwner, draggedIndex);
                if (cchanel != null)
                    Sqllite.UpdateChanelOrder(Subscribe.ChanelDb, cchanel.ChanelOwner, targetIndex);

                //select the dropped item
                ViewModelLocator.MvViewModel.Model.MySubscribe.CurrentChanel = DraggedItem;
            }

            //reset
            ResetDragDrop();
        }

        private void OnMouseMoveChanells(object sender, MouseEventArgs e)
        {
            if (!IsDragging || e.LeftButton != MouseButtonState.Pressed) return;

            ////display the popup if it hasn't been opened yet
            //if (!Popup1.IsOpen)
            //{
            //    //switch to read-only mode
            //    DataGridChanels.IsReadOnly = true;

            //    //make sure the popup is visible
            //    Popup1.IsOpen = true;
            //}


            //Size popupSize = new Size(Popup1.ActualWidth, Popup1.ActualHeight);
            //Popup1.PlacementRectangle = new Rect(e.GetPosition(this), popupSize);

            //make sure the row under the grid is being selected
            Point position = e.GetPosition(DataGridChanels);
            var row = UIHelpers.TryFindFromPoint<DataGridRow>(DataGridChanels, position);
            if (row != null) 
                DataGridChanels.SelectedItem = row.Item;
        }

        private void OnMouseLeftButtonDownChanells(object sender, MouseButtonEventArgs e)
        {
            if (IsEditing) return;

            var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(DataGridChanels));
            if (row == null || row.IsEditing) 
                return;

            //set flag that indicates we're capturing mouse movements
            IsDragging = true;
            DraggedItem = (ChanelBase)row.Item;
        }

        private void OnEndEditChanells(object sender, DataGridCellEditEndingEventArgs e)
        {
            IsEditing = false;
        }

        private void OnBeginEditChanells(object sender, DataGridBeginningEditEventArgs e)
        {
            IsEditing = true;
            //in case we are in the middle of a drag/drop operation, cancel it...
            if (IsDragging) 
                ResetDragDrop();
        }

        private void ResetDragDrop()
        {
            IsDragging = false;
            DataGridChanels.IsReadOnly = false;
        }
    }
}
