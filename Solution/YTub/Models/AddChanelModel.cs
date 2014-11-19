using System;
using System.Collections.Generic;
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

        public RelayCommand AddChanelCommand { get; set; }

        public AddChanelView View { get; set; }

        public string ChanelName { get; set; }

        public string ChanelOwner { get; set; }

        public AddChanelModel(AddChanelView view, bool isedit)
        {
            _isedit = isedit;
            View = view;
            AddChanelCommand = new RelayCommand(AddChanel);
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
                    var chanel = new Chanel(ChanelName, ChanelOwner);
                    ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelList.Add(chanel);
                    ViewModelLocator.MvViewModel.Model.MySubscribe.ChanelListToBind.Add(chanel);
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
