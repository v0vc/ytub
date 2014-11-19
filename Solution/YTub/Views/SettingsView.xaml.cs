using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace YTub.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();
            KeyDown += SettingsView_KeyDown;
        }

        void SettingsView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyDown -= SettingsView_KeyDown;
                Close();
            }
            if (e.Key == Key.Enter)
            {
                //лень вызывать вьюмодельлокатор, я в этих мелких диалогах отказался от вьюмодели, нажмем кнопку программно
                var peer = new ButtonAutomationPeer(ButtonSave); 
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                if (invokeProv != null) 
                    invokeProv.Invoke();
            }
        }

        private void SyncOnStart(object sender, MouseButtonEventArgs e)
        {
            CheckBoxSync.IsChecked = !CheckBoxSync.IsChecked;
        }

        private void ShowFavorites(object sender, MouseButtonEventArgs e)
        {
            CheckBoxFavor.IsChecked = !CheckBoxFavor.IsChecked;
        }
    }
}
