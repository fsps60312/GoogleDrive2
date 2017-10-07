using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GoogleDrive2.UWP.MyControls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WebViewWindow : Page
    {
        public static WebViewWindow Instance = null;
        Button BTNrefresh,BTNback,BTNforward;
        Grid GDmain;
        public WebView webView;
        private void InitializeViews()
        {
            GDmain = new Grid();
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            {
                BTNrefresh = new Button { Content = "↻" };
                GDmain.Children.Add(BTNrefresh);
            }
            {
                BTNback = new Button { Content = "←" };
                GDmain.Children.Add(BTNback);
                Grid.SetColumn(BTNback, 1);
            }
            {
                BTNforward = new Button { Content = "→" };
                GDmain.Children.Add(BTNforward);
                Grid.SetColumn(BTNforward, 2);
            }
            {
                webView = new WebView
                {
                    Source = new Uri("http://codingsimplifylife.blogspot.tw/")
                };
                GDmain.Children.Add(webView);
                Grid.SetRow(webView, 1);
                Grid.SetColumnSpan(webView, GDmain.ColumnDefinitions.Count);
            }
            this.Content = GDmain;
        }
        private void RegisterEvents()
        {
            BTNrefresh.Click += async delegate
            {
                BTNrefresh.IsEnabled = false;
                webView.Refresh();
                await Task.Delay(500);
                BTNrefresh.IsEnabled = true;
            };
            BTNback.Click += async delegate
            {
                BTNback.IsEnabled = false;
                if (webView.CanGoBack) webView.GoBack();
                else BTNback.Content = "💀";
                await Task.Delay(500);
                BTNback.Content = "←";
                BTNback.IsEnabled = true;
            };
            BTNforward.Click += async delegate
            {
                BTNforward.IsEnabled = false;
                if (webView.CanGoForward) webView.GoForward();
                else BTNforward.Content = "💀";
                await Task.Delay(500);
                BTNforward.Content = "→";
                BTNforward.IsEnabled = true;
            };
        }
        public WebViewWindow()
        {
            Instance = this;
            InitializeViews();
            RegisterEvents();
        }
    }
}
