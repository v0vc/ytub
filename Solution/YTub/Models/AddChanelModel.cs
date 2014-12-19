using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YTub.Chanell;
using YTub.Common;
using YTub.Video;
using YTub.Views;

namespace YTub.Models
{
    public class AddChanelModel
    {
        private readonly MainWindowModel _model;

        private readonly bool _isedit;

        public ObservableCollection<ChanelBase> ServerList { get; set; }

        public RelayCommand AddChanelCommand { get; set; }

        public AddChanelView View { get; set; }

        public string ChanelName { get; set; }

        public string ChanelOwner { get; set; }

        public ChanelBase SelectedForumItem { get; set; }

        public AddChanelModel(MainWindowModel model, AddChanelView view, bool isedit, ObservableCollection<ChanelBase> serverList)
        {
            _model = model;
            _isedit = isedit;
            View = view;
            AddChanelCommand = new RelayCommand(AddChanel);
            ServerList = serverList;
            if (ServerList.Any())
                SelectedForumItem = ServerList[0];

            var text = Clipboard.GetData(DataFormats.Text) as string;
            if (string.IsNullOrWhiteSpace(text) || text.Contains(Environment.NewLine))
                return;
            ChanelOwner = text;
        }

        private void AddChanel(object o)
        {
            try
            {
                if (_isedit)
                {
                    var chanel = _model.MySubscribe.ChanelList.FirstOrDefault(x => x.ChanelOwner == ChanelOwner);
                    if (chanel != null)
                    {
                        chanel.ChanelName = ChanelName;
                        Sqllite.UpdateChanelName(Subscribe.ChanelDb, ChanelName, ChanelOwner);
                    }
                }
                else
                {
                    var ordernum = _model.MySubscribe.ChanelList.Count;
                    ChanelBase chanel = null;
                    if (string.IsNullOrEmpty(ChanelName))
                        ChanelName = ChanelOwner;
                    if (SelectedForumItem.ChanelType == "YouTube")
                        chanel = new ChanelYou(SelectedForumItem.ChanelType, SelectedForumItem.Login, SelectedForumItem.Password,ChanelName, ChanelOwner, ordernum);
                    if (SelectedForumItem.ChanelType == "RuTracker")
                        chanel = new ChanelRt(SelectedForumItem.ChanelType, SelectedForumItem.Login, SelectedForumItem.Password, ChanelName, ChanelOwner, ordernum);
                    if (SelectedForumItem.ChanelType == "Tapochek")
                        chanel = new ChanelTap(SelectedForumItem.ChanelType, SelectedForumItem.Login, SelectedForumItem.Password, ChanelName, ChanelOwner, ordernum);
                    if (chanel != null)
                    {
                        if (!_model.MySubscribe.ChanelList.Select(z => z.ChanelOwner).Contains(ChanelOwner))
                        {
                            _model.MySubscribe.ChanelList.Add(chanel);
                            _model.MySubscribe.ChanelListToBind.Add(chanel);
                            chanel.IsFull = true;
                            chanel.GetItemsFromNet();
                            _model.MySubscribe.CurrentChanel = chanel;
                            _model.MySubscribe.SelectedTabIndex = 0;

                        }
                        else
                        {
                            MessageBox.Show("Subscribe has already " + ChanelOwner, "Information", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
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
