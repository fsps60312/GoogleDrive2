using System;
using System.Threading.Tasks;
using GoogleDrive2.MyControls;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.HttpStatusPage
{
    class HttpStatusBar : MyControls.BarsListPanel.DataBindedGrid<HttpStatusBarViewModel>
    {
        MyLabel LBuri, LBstatus;
        MyScrollView SVstatus;
        MyButton BTNdetail;
        MyProgressBar PBprogress;
        public HttpStatusBar()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Absolute) });
            this.RowSpacing = 0;
            {
                PBprogress = new MyProgressBar();
                PBprogress.SetBinding(MyProgressBar.ProgressProperty, "Progress");
                this.Children.Add(PBprogress, 0, 1);
                MyGrid.SetColumnSpan(PBprogress, this.ColumnDefinitions.Count);
                //MyGrid.SetRowSpan(PBprogress, this.RowDefinitions.Count);
            }
            {
                LBuri = new MyLabel
                {
                    LineBreakMode = LineBreakMode.MiddleTruncation
                };
                //LBuri.Opacity = 0.75;
                LBuri.SetBinding(MyLabel.TextProperty, "Uri");
                LBuri.SetBinding(MyLabel.BackgroundColorProperty, "Color");
                this.Children.Add(LBuri, 0, 0);
            }
            {
                SVstatus = new MyScrollView { Orientation = ScrollOrientation.Horizontal };
                SVstatus.LayoutChanged += async delegate { await SVstatus.ScrollToAsync(LBstatus, ScrollToPosition.End, true); };
                {
                    LBstatus = new MyLabel();
                    LBstatus.SetBinding(MyLabel.TextProperty, "Status");
                    LBstatus.LineBreakMode = LineBreakMode.HeadTruncation;
                    SVstatus.Content = LBstatus;
                }
                this.Children.Add(SVstatus, 1, 0);
            }
            {
                BTNdetail = new MyButton();
                BTNdetail.SetBinding(MyButton.TextProperty, "Icon");
                BTNdetail.SetBinding(MyButton.CommandProperty, "Clicked");
                this.Children.Add(BTNdetail, 2, 0);
            }
        }
    }
}
