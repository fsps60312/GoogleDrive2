using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class FileUploadBarsList:MyControls.BarsListPanel.BarsListPanel<FileUploadBar,FileUploadBarViewModel>
    {
        public FileUploadBarsList()
        {
            Local.File.Uploader.NewUploaderCreated += (uploader) =>
              {
                  this.PushBack(new FileUploadBarViewModel(uploader));
              };
        }
    }
    class FileUploadBar:MyControls.BarsListPanel.DataBindedGrid<FileUploadBarViewModel>
    {
        MyLabel LBicon, LBname, LBuploaded,LBtotal,LBpercentage,LBspeed,LBtimeRemaining,LBtimePassed;
        MyButton BTNinfo;
        MyImage IMGspeedGraph;
        MyProgressBar PBprogress;
        private void SetBindings()
        {
            LBicon.SetBinding(MyLabel.TextProperty, "Icon");
            LBname.SetBinding(MyLabel.TextProperty, "Name");
            LBuploaded.SetBinding(MyLabel.TextProperty, "Uploaded");
            LBtotal.SetBinding(MyLabel.TextProperty, "Total");
            LBpercentage.SetBinding(MyLabel.TextProperty, "Percentage");
            LBspeed.SetBinding(MyLabel.TextProperty, "Speed");
            LBtimeRemaining.SetBinding(MyLabel.TextProperty, "TimeRemaining");
            LBtimePassed.SetBinding(MyLabel.TextProperty, "TimePassed");
            BTNinfo.SetBinding(MyButton.TextProperty, "Info");
            BTNinfo.SetBinding(MyButton.CommandProperty, "InfoClicked");
            BTNinfo.SetBinding(MyButton.IsEnabledProperty, "InfoEnabled");
            PBprogress.SetBinding(MyProgressBar.ProgressProperty, "Progress");
            IMGspeedGraph.SetBinding(MyImage.SourceProperty, "SpeedGraph");
        }
        private void ArrangeViews()
        {
            this.RowSpacing = 0;
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Absolute) });//icon
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//name
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//percentage, speed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//uploaded, time passed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//total, time remaining
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//info
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Absolute) });
            {
                this.Children.Add(PBprogress, 3, 0);
                MyGrid.SetColumnSpan(PBprogress, 2);
                this.Children.Add(IMGspeedGraph, 3, 0);
                MyGrid.SetColumnSpan(IMGspeedGraph, 2);
                this.Children.Add(LBicon, 0, 0);
                MyGrid.SetRowSpan(LBicon, 2);
                this.Children.Add(LBname, 1, 0);
                MyGrid.SetRowSpan(LBname, 2);
                this.Children.Add(LBpercentage, 2, 0);
                this.Children.Add(LBspeed, 2, 1);
                this.Children.Add(LBtimePassed, 3, 0);
                this.Children.Add(LBuploaded, 3, 1);
                this.Children.Add(LBtimeRemaining, 4, 0);
                this.Children.Add(LBtotal, 4, 1);
                this.Children.Add(BTNinfo, 5, 0);
                MyGrid.SetRowSpan(BTNinfo, 2);
            }
        }
        private void InitializeViews()
        {
            {
                LBicon = new MyLabel();
                LBname = new MyLabel();
                LBuploaded = new MyLabel();
                LBtotal = new MyLabel();
                LBpercentage = new MyLabel();
                LBspeed = new MyLabel();
                LBtimeRemaining = new MyLabel();
                LBtimePassed = new MyLabel();
                BTNinfo = new MyButton();
                PBprogress = new MyProgressBar();
                IMGspeedGraph = new MyImage {Aspect=Aspect.Fill };
            }
        }
        public FileUploadBar()
        {
            InitializeViews();
            ArrangeViews();
            SetBindings();
        }
    }
    class FileUploadPage:MyContentPage
    {
        public FileUploadPage()
        {
            this.Title = "File Upload";
            this.Content = new FileUploadBarsList();
        }
    }
}
