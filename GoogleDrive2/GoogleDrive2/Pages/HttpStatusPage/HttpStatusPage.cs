using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.Linq;

namespace GoogleDrive2.Pages.HttpStatusPage
{
    class HttpStatusPanel : MyControls.BarsListPanel.BarsListPanel<HttpStatusBar, HttpStatusBarViewModel>
    {
        public HttpStatusPanel()
        {
            MyHttpRequest.NewRequestCreated += (r) =>
            {
                this.PushBack(new HttpStatusBarViewModel(r));
            };
        }
    }

    partial class HttpStatusPage : MyContentPage
    {
        MyGrid GDmain;
        MyLabel LBrequest, LBresponse,LBfile;
        MyButton BTNclear,BTNmemory;
        HttpStatusPanel NSPmain;
        HttpStatistics httpStatistics = new HttpStatistics();
        void InitializeViews()
        {
            this.Title = "Http Status";
            {
                GDmain = new MyGrid();
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
                    BTNmemory = new MyButton
                    {
                        FontFamily="Consolas",
                        BindingContext = httpStatistics
                    };
                    BTNmemory.SetBinding(MyButton.TextProperty, "MemoryUsed");
                    GDmain.Children.Add(BTNmemory, 3, 0);
                }
                {
                    BTNclear = new MyButton { Text = Constants.Icons.Clear + "Clear" };
                    GDmain.Children.Add(BTNclear, 4, 0);
                }
                {
                    NSPmain = new HttpStatusPanel();
                    GDmain.Children.Add(NSPmain, 0, 1);
                    MyGrid.SetColumnSpan(NSPmain, GDmain.ColumnDefinitions.Count);
                }
                this.Content = GDmain;
            }
        }
        void RegisterEvents()
        {
            BTNmemory.Clicked += delegate
              {
                  BTNmemory.IsEnabled = false;
                  System.GC.Collect(System.GC.MaxGeneration, System.GCCollectionMode.Forced, true);
                  BTNmemory.IsEnabled = true;
              };
            BTNclear.Clicked += async delegate
            {
                BTNmemory.IsEnabled = false;
                if (await MyLogger.Ask("Clear all http requests/responses history.\r\nAre you sure?")) await NSPmain.ClearAsync();
                BTNmemory.IsEnabled = true;
            };
        }
        public HttpStatusPage()
        {
            InitializeViews();
            RegisterEvents();
        }
    }
}
