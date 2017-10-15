using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.ComponentModel;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class NetworkStatusPanel : MyControls.BarsListPanel.BarsListPanel<NetworkStatusBar, NetworkStatusBarViewModel>
    {
        public NetworkStatusPanel()
        {
            MyHttpRequest.NewRequestCreated += (r) =>
            {
                this.PushBack(new NetworkStatusBarViewModel(r));
            };
        }
    }
    class NetworkStatusPage : MyContentPage
    {
        MyGrid GDmain;
        MyLabel LBrequest, LBresponse,LBfile;
        MyButton BTNclear;
        NetworkStatusPanel NSPmain;
        HttpStatistics httpStatistics = new HttpStatistics();
        void InitializeViews()
        {
            this.Title = "Network Status";
            {
                GDmain = new MyGrid();
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                {
                    LBrequest = new MyLabel
                    {
                        BindingContext = httpStatistics
                    };
                    LBrequest.SetBinding(MyLabel.TextProperty, "RequestCount");
                    GDmain.Children.Add(LBrequest, 0, 0);
                }
                {
                    LBresponse = new MyLabel
                    {
                        BindingContext = httpStatistics
                    };
                    LBresponse.SetBinding(MyLabel.TextProperty, "ResponseCount");
                    GDmain.Children.Add(LBresponse, 1, 0);
                }
                {
                    LBfile = new MyLabel
                    {
                        BindingContext = httpStatistics
                    };
                    LBfile.SetBinding(MyLabel.TextProperty, "FileCount");
                    GDmain.Children.Add(LBfile, 2, 0);
                }
                {
                    BTNclear = new MyButton { Text = Constants.Icons.Clear + "Clear" };
                    GDmain.Children.Add(BTNclear, 3, 0);
                }
                {
                    NSPmain = new NetworkStatusPanel();
                    GDmain.Children.Add(NSPmain, 0, 1);
                    MyGrid.SetColumnSpan(NSPmain, GDmain.ColumnDefinitions.Count);
                }
                this.Content = GDmain;
            }
        }
        void RegisterEvents()
        {
            BTNclear.Clicked += async delegate {if(await MyLogger.Ask("Clear all http requests/responses history.\r\nAre you sure?")) await NSPmain.ClearAsync(); };
        }
        public NetworkStatusPage()
        {
            InitializeViews();
            RegisterEvents();
        }

        class HttpStatistics : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            string __RequestCount__ = "Active Requests: Unknown";
            string __ResponseCount__ = "Active Responses: Unknown";
            string __FileCount__ = "Opened Files: Unknown";
            public string RequestCount
            {
                get { return __RequestCount__; }
                set
                {
                    if (__RequestCount__ == value) return;
                    __RequestCount__ = value;
                    OnPropertyChanged("RequestCount");
                }
            }
            public string ResponseCount
            {
                get { return __ResponseCount__; }
                set
                {
                    if (__ResponseCount__ == value) return;
                    __ResponseCount__ = value;
                    OnPropertyChanged("ResponseCount");
                }
            }
            public string FileCount
            {
                get { return __FileCount__; }
                set
                {
                    if (__FileCount__ == value) return;
                    __FileCount__ = value;
                    OnPropertyChanged("FileCount");
                }
            }
            public HttpStatistics()
            {
                MyHttpRequest.InstanceCountChanged += (c) => { RequestCount= $"Active Requests: {c}"; };
                MyHttpResponse.InstanceCountChanged += (c) => { ResponseCount= $"Active Responses: {c}"; };
                Local.File.InstanceCountChanged += (c) => { FileCount = $"Opened Files: {c}"; };
            }
        }
    }
}
