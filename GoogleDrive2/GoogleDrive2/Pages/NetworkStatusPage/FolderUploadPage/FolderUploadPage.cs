using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.NetworkStatusPage.FolderUploadPage
{
    class FolderUploadBarsList:MyControls.BarsListPanel.BarsListPanel<FolderUploadBar,FolderUploadBarViewModel>
    {
        public FolderUploadBarsList()
        {
            Local.Folder.Uploader.NewUploaderCreated += (uploader) =>
              {
                  this.PushFront(new FolderUploadBarViewModel(uploader));
              };
        }
    }
    static class FolderUploadBar_Extensions
    {
        public static void AddChildrenAndSetSpan(this MyGrid grid, View view, int left, int top, int columnSpan, int rowSpan)
        {
            grid.Children.Add(view, left, top);
            MyGrid.SetColumnSpan(view, columnSpan);
            MyGrid.SetRowSpan(view, rowSpan);
        }
        public static void AddChildrenAndFillHeight(this MyGrid grid, View view, int left)
        {
            grid.Children.Add(view, left, 0);
            MyGrid.SetRowSpan(view, Math.Max(1, grid.RowDefinitions.Count));
        }
    }
    class FolderUploadBar:MyControls.BarsListPanel.DataBindedGrid<FolderUploadBarViewModel>
    {
        MyLabel LBicon, LBname, LBpercentage, LBcurrentSize, LBtotalSize,LBcurrentFile,LBtotalFile,LBcurrentFolder,LBtotalFolder, LBspeed, LBtimeRemaining, LBtimePassed;
        MyButton BTNinfo, BTNpause;
        MyImage IMGspeedGraph;
        MyProgressBar PBprogress,PBsizeProgress,PBfileProgress,PBfolderProgress;
        private void SetBindings()
        {
            LBicon.SetBinding(MyLabel.TextProperty, "Icon");
            LBname.SetBinding(MyLabel.TextProperty, "Name");
            LBcurrentSize.SetBinding(MyLabel.TextProperty, "CurrentSize");
            LBtotalSize.SetBinding(MyLabel.TextProperty, "TotalSize");
            LBcurrentFile.SetBinding(MyLabel.TextProperty, "CurrentFile");
            LBtotalFile.SetBinding(MyLabel.TextProperty, "TotalFile");
            LBcurrentFolder.SetBinding(MyLabel.TextProperty, "CurrentFolder");
            LBtotalFolder.SetBinding(MyLabel.TextProperty, "TotalFolder");
            LBpercentage.SetBinding(MyLabel.TextProperty, "ProgressText");
            LBspeed.SetBinding(MyLabel.TextProperty, "Speed");
            LBtimeRemaining.SetBinding(MyLabel.TextProperty, "TimeRemaining");
            LBtimePassed.SetBinding(MyLabel.TextProperty, "TimePassed");
            BTNinfo.SetBinding(MyButton.TextProperty, "Info");
            BTNinfo.SetBinding(MyButton.CommandProperty, "InfoClicked");
            BTNinfo.SetBinding(MyButton.IsEnabledProperty, "InfoEnabled");
            PBprogress.SetBinding(MyProgressBar.ProgressProperty, "Progress");
            IMGspeedGraph.SetBinding(MyImage.SourceProperty, "SpeedGraph");
            BTNpause.SetBinding(MyButton.TextProperty, "PauseButtonText");
            BTNpause.SetBinding(MyButton.CommandProperty, "PauseClicked");
            BTNpause.SetBinding(MyButton.IsEnabledProperty, "PauseButtonEnabled");
        }
        private void ArrangeViews()
        {
            this.RowSpacing = 0.5;
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Absolute) });//icon
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//name
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//percentage, speed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//uploaded, time passed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//total, time remaining
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//info
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Absolute) });//pause button
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15, GridUnitType.Absolute) });
            {
                this.AddChildrenAndSetSpan(PBprogress, 3, 0, 2, 1);
                this.AddChildrenAndSetSpan(PBsizeProgress, 3, 1, 2, 1);
                this.AddChildrenAndSetSpan(IMGspeedGraph, 3, 1, 2, 1);
                this.AddChildrenAndSetSpan(PBfileProgress, 3, 2, 2, 1);
                this.AddChildrenAndSetSpan(PBfolderProgress, 3, 3, 2, 1);
                this.AddChildrenAndFillHeight(LBicon, 0);
                this.AddChildrenAndFillHeight(LBname, 1);
                this.Children.Add(LBpercentage, 2, 0);
                this.Children.Add(LBtimePassed, 3, 0);
                this.Children.Add(LBtimeRemaining, 4, 0);
                this.Children.Add(LBspeed, 2, 1);
                this.Children.Add(LBcurrentSize, 3, 1);
                this.Children.Add(LBtotalSize, 4, 1);
                this.Children.Add(LBcurrentFile, 3, 2);
                this.Children.Add(LBtotalFile, 4, 2);
                this.Children.Add(LBcurrentFolder, 3, 3);
                this.Children.Add(LBtotalFolder, 4, 3);
                this.AddChildrenAndFillHeight(BTNinfo, 5);
                this.AddChildrenAndFillHeight(BTNpause, 6);
            }
        }
        private void InitializeViews()
        {
            {
                LBicon = new MyLabel();
                LBname = new MyLabel();
                LBcurrentSize = new MyLabel();
                LBtotalSize = new MyLabel();
                LBcurrentFile = new MyLabel();
                LBtotalFile = new MyLabel();
                LBcurrentFolder = new MyLabel();
                LBtotalFolder = new MyLabel();
                LBpercentage = new MyLabel();
                LBspeed = new MyLabel();
                LBtimeRemaining = new MyLabel();
                LBtimePassed = new MyLabel();
                BTNinfo = new MyButton();
                PBprogress = new MyProgressBar();
                IMGspeedGraph = new MyImage { Aspect = Aspect.Fill };
                BTNpause = new MyButton();
            }
        }
        public FolderUploadBar()
        {
            InitializeViews();
            ArrangeViews();
            SetBindings();
        }
    }
    class FolderUploadPage:MyContentPage
    {
        public FolderUploadPage()
        {
            this.Title = "Folder Upload";
            this.Content = new FolderUploadBarsList();
        }
    }
}
