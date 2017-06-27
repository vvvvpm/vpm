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
using MahApps.Metro.Controls;
using CefSharp;
using CefSharp.Wpf;

namespace vpm
{
    /// <summary>
    /// Interaction logic for UserAgree.xaml
    /// </summary>
    public partial class UserAgree : MetroWindow
    {
        public JsVPackInterop InteropObj;
        public ListBoxItem SelectedPack;
        public bool PackChanged = false;
        public UserAgree()
        {
            InitializeComponent();
        }

        public void DisableAgree()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                AgreeAndInstall.IsEnabled = false;
            }));
        }
        public void EnableAgree()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                AgreeAndInstall.IsEnabled = true;
            }));
        }

        public void ContinueFromJS()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                AgreeAndInstall.IsEnabled = true;
                AgreeAndInstall.IsChecked = true;
                if (VPackList.SelectedIndex < VPackList.Items.Count - 1)
                    VPackList.SelectedIndex = (VPackList.SelectedIndex + 1) % VPackList.Items.Count;
            }));
        }
        private void ContinueInstall_Click(object sender, RoutedEventArgs e)
        {
            VpmConfig.Instance.InstallationCancelled = false;
            VpmConfig.Instance.WaitSignal = false;
            VpmConfig.Instance.AgreementsAgreed = true;
            VpmConfig.Instance.AgreeWindow.Close();
        }

        private void UserAgree_OnInitialized(object sender, EventArgs e)
        {

            InteropObj = new JsVPackInterop
            {
                UserAgreeWindow = this
            };
            Browser.RegisterJsObject("vpm", InteropObj);
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
            var delay = new Timer { Interval = 1000 };
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
            PackChanged = true;
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
            if (ContinueInstall.IsEnabled) ContinueInstall.Opacity = 1;

        }

        private void Browser_OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action( () =>
            {
                if (PackChanged)
                {
                    if (e.IsLoading)
                    {
                        LoadingCover.Visibility = Visibility.Visible;
                        LoadingRing.IsActive = true;
                        AgreeAndInstall.IsEnabled = false;
                        InteropObj.CurrentPack = (VPack)SelectedPack.Content;
                    }
                    else
                    {
                        LoadingCover.Visibility = Visibility.Hidden;
                        LoadingRing.IsActive = false;
                        AgreeAndInstall.IsEnabled = true;
                        NextPack.IsEnabled = true;
                        PackChanged = false;
                    }
                }
                else
                {
                    SmallLoadingRing.IsActive = e.IsLoading;
                }
            }));
        }

        private void NextPack_OnClick(object sender, RoutedEventArgs e)
        {
            VPackList.SelectedIndex = (VPackList.SelectedIndex + 1) % VPackList.Items.Count;
        }

        private void AgreeAndInstall_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) AgreeAndInstall.Opacity = 1;
            else AgreeAndInstall.Opacity = 0.5;
        }
    
        private void OnCancelled(object sender, EventArgs e)
        {
            VpmConfig.Instance.InstallationCancelled = true;
            VpmConfig.Instance.WaitSignal = false;
            VpmConfig.Instance.AgreementsAgreed = true;
            VpmConfig.Instance.DirWindow.Close();
        }
    }
}
