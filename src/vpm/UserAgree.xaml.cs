using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Wpf;

namespace vpm
{
    /// <summary>
    /// Interaction logic for UserAgree.xaml
    /// </summary>
    public partial class UserAgree : Window
    {
        public ListBoxItem SelectedPack;
        public UserAgree()
        {
            InitializeComponent();
        }

        private void ContinueInstall_Click(object sender, RoutedEventArgs e)
        {
            VpmConfig.Instance.InstallationCancelled = false;
            VpmConfig.Instance.AgreeWindow.Close();
            VpmConfig.Instance.WinApp.Shutdown();
        }

        private void UserAgree_OnInitialized(object sender, EventArgs e)
        {
            foreach (var vpack in VpmConfig.Instance.PackList)
            {
                var item = new ListBoxItem
                {
                    Content = vpack,
                    Background = new SolidColorBrush
                    {
                        Color = Color.FromRgb(255, 0, 0)
                    },
                };
                VPackList.Items.Add(item);
            }
            var delay = new Timer { Interval = 1500 };
            delay.Elapsed += (o, ee) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    VPackList.SelectedIndex = 0;
                    delay.Stop();
                }));
            };
            delay.Start();
        }

        private void VPackList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count <= 0) return;

            SelectedPack = (ListBoxItem)e.AddedItems[0];
            var pack = (VPack)SelectedPack.Content;
            AgreeAndInstall.IsChecked = pack.Agreed;
            Browser.Load(pack.LicenseUrl);
        }

        private void AgreeAndInstall_OnChecked(object sender, RoutedEventArgs e)
        {
            SelectedPack.Background = new SolidColorBrush
            {
                Color = Color.FromRgb(0, 255, 0)
            };
            var pack = (VPack)SelectedPack.Content;
            pack.Agreed = true;

            ContinueInstall.IsEnabled = (from ListBoxItem item in VPackList.Items select (VPack) item.Content).Aggregate(true, (current, ipack) => current && ipack.Agreed);

        }

        private void Browser_OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action( () =>
            {
                if (e.IsLoading)
                {
                    AgreeAndInstall.IsEnabled = false;
                    NextPack.IsEnabled = false;
                }
                else
                {
                    AgreeAndInstall.IsEnabled = true;
                    NextPack.IsEnabled = true;
                }
            }));
        }

        private void NextPack_OnClick(object sender, RoutedEventArgs e)
        {
            VPackList.SelectedIndex = (VPackList.SelectedIndex + 1) % VPackList.Items.Count;
        }
    }
}
