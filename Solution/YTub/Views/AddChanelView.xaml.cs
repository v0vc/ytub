using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace YTub.Views
{
    /// <summary>
    /// Interaction logic for AddChanelView.xaml
    /// </summary>
    public partial class AddChanelView : Window
    {
        public AddChanelView()
        {
            InitializeComponent();
            KeyDown += ShowCommentPicWindow_KeyDown;
        }
        void ShowCommentPicWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyDown -= ShowCommentPicWindow_KeyDown;
                Close();
            }
            if (e.Key == Key.Enter)
            {
                //нажмем кнопку программно
                var peer = new ButtonAutomationPeer(ButtonAdd);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                if (invokeProv != null)
                    invokeProv.Invoke();
            }
        }

        private void AddChanelView_OnLoaded(object sender, RoutedEventArgs e)
        {
            TextBoxLink.SelectAll();
        }
    }
}
