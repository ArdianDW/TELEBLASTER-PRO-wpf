using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TELEBLASTER_PRO.Views.UserControls
{
    /// <summary>
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : UserControl
    {
        public Sidebar()
        {
            InitializeComponent();
        }

        private void NavigateToAccounts(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new Accounts();
        }

        private void NavigateToSendMessage(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new SendMessage();
        }

        private void NavigateToClickToChat(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new ClickToChat();
        }

        private void NavigateToGroupChannel(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new GroupChannel();
        }

        private void NavigateToGroupFinder(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new GroupFinder();
        }

        private void NavigateToInviteGroupChannel(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new InviteGroupChannel();
        }

        private void NavigateToNumberGenerator(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new NumberGenerator();
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainContentControl.Content = new Settings();
        }
    }
}
