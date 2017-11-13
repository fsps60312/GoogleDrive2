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
        MyButton BTNclear;
        HttpStatusPanel NSPmain;
        void InitializeViews()
        {
            this.Title = "Http Status";
            {
                GDmain = new MyGrid();
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                {
                    BTNclear = new MyButton { Text = Constants.Icons.Clear + "Clear" };
                    GDmain.Children.Add(BTNclear, 1, 0);
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
            BTNclear.Clicked += async delegate
            {
                BTNclear.IsEnabled = false;
                if (await MyLogger.Ask("Clear all http requests/responses history.\r\nAre you sure?")) await NSPmain.ClearAsync();
                BTNclear.IsEnabled = true;
            };
        }
        public HttpStatusPage()
        {
            InitializeViews();
            RegisterEvents();
        }
    }
}
