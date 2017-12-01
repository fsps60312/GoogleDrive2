using System;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanel
    {
        public class CloudFileItemBar : BarsListPanel.DataBindedGrid<CloudFileListPanelViewModel.CloudFileItemBarViewModel>
        {
            MyButton BTNmain;
            public CloudFileItemBar()
            {
                //this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Absolute) });
                //this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                //this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Absolute) });
                //this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Absolute) });
                //this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                //this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Absolute) });
                {
                    //var bx = new MyBoxView();
                    //bx.SetBinding(MyBoxView.BackgroundColorProperty, "BorderColor");
                    //this.Children.Add(bx, 0, 0);
                    //MyGrid.SetColumnSpan(bx, this.ColumnDefinitions.Count);
                    //MyGrid.SetRowSpan(bx, this.RowDefinitions.Count);
                    this.SetBinding(MyGrid.BackgroundColorProperty, "BorderColor");
                }
                double? a = 0;
                a += 1;
                {
                    BTNmain = new MyButton();
                    BTNmain.SetBinding(MyButton.OpacityProperty, "Opacity");
                    BTNmain.SetBinding(MyButton.TextProperty, "Text");
                    BTNmain.SetBinding(MyButton.CommandProperty, "Clicked");
                    BTNmain.SetBinding(MyButton.BackgroundColorProperty, "BackgroundColor");
                    this.Children.Add(BTNmain, 0, 0);
                }
            }
        }
    }
}
