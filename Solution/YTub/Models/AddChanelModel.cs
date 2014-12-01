using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YTub.Common;
using YTub.Views;

namespace YTub.Models
{
    public class AddChanelModel
    {
        private readonly bool _isedit;

        public ObservableCollection<ForumItem> ServerList { get; set; }

        public RelayCommand AddChanelCommand { get; set; }

        public AddChanelView View { get; set; }

        public string ChanelName { get; set; }

        public string ChanelOwner { get; set; }

        public ForumItem SelectedForumItem { get; set; }

        public AddChanelModel(AddChanelView view, bool isedit, ObservableCollection<ForumItem> serverList)
        {
            _isedit = isedit;
            View = view;
            AddChanelCommand = new RelayCommand(AddChanel);
            ServerList = serverList;
            if (ServerList.Any())
                SelectedForumItem = ServerList[0];
            //Name = "Best of the Web";
            //User = "zapatou";
            //Name = "Den";
            //User = "fit4liferu";
        }

        private void AddChanel(object o)
        {
            try
            {
                if (_isedit)
                {
                    var chanel = ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.FirstOrDefault(x => x.ChanelOwner == ChanelOwner);
                    if (chanel != null)
                    {
                        chanel.ChanelName = ChanelName;
                        Sqllite.UpdateChanelName(Subscribe.ChanelDb, ChanelName, ChanelOwner);
                    }
                }
                else
                {
                    var ordernum = ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Count;
                    var chanel = new Chanel(ChanelName, ChanelOwner, SelectedForumItem.ForumName, ordernum);
                    if (!ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Select(z => z.ChanelOwner).Contains(ChanelOwner))
                    {
                        ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Add(chanel);
                        ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.Add(chanel);
                    }
                    else
                    {
                        MessageBox.Show("Subscribe has already " + ChanelOwner, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    //ViewModelLocator.MvViewModel.Model.MySubscribe.IsOnlyFavorites = false;
                }
                View.Close();    
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Oops", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
