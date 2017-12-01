using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.NetworkStatusPage.FolderUploadPage
{
    class FolderUploadBarsList : MyControls.BarsListPanel.BarsListPanel<FolderUploadBar, FolderUploadBarViewModel>
    {
        void Fold(FolderUploadBarViewModel fubv)
        {
            if (fubv.IsFolded) return;
            this.DoAtomic(() =>
            {
                foreach (var child in Children[fubv]) Fold(child);
                foreach (var child in Children[fubv]) child.OnDisposed();
                fubv.IsFolded = true;
            });
        }
        void Unfold(FolderUploadBarViewModel fubv)
        {
            if (!fubv.IsFolded) return;
            this.DoAtomic(() =>
            {
                var position = Treap.QueryPosition(TreapNode[fubv]);
                foreach (var child in Children[fubv])
                {
                    TreapNode[child] = this.Insert(child, ++position);
                }
                fubv.IsFolded = false;
            });
        }
        Dictionary<FolderUploadBarViewModel, List<FolderUploadBarViewModel>> Children = new Dictionary<FolderUploadBarViewModel, List<FolderUploadBarViewModel>>();
        Dictionary<Local.Folder.Uploader, FolderUploadBarViewModel> VM = new Dictionary<Local.Folder.Uploader, FolderUploadBarViewModel>();
        Dictionary<FolderUploadBarViewModel, MyControls.BarsListPanel.Treap<FolderUploadBarViewModel>.TreapNodePrototype> TreapNode = new Dictionary<FolderUploadBarViewModel, MyControls.BarsListPanel.Treap<FolderUploadBarViewModel>.TreapNodePrototype>();
        public FolderUploadBarsList()
        {
            this.ItemHeight = 65;
            Local.Folder.Uploader.NewUploaderCreated += (uploader) =>
              {
                  var vm = new FolderUploadBarViewModel(uploader, Unfold, Fold);
                  if (!VM.ContainsKey(uploader)) VM[uploader] = vm;
                  if (!Children.ContainsKey(vm)) Children.Add(vm, new List<FolderUploadBarViewModel>());
                  if (uploader.Parent != null)
                  {
                      Children[VM[uploader.Parent]].Add(vm);
                      VM[uploader.Parent].IsFoldEnabled = true;
                  }
                  else
                  {
                      TreapNode[vm] = this.PushBack(vm);
                  }
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
    class FolderUploadBar : MyControls.BarsListPanel.DataBindedGrid<FolderUploadBarViewModel>
    {
        MyLabel LBname, LBpercentage, LBcurrentSize, LBtotalSize, LBfileStatus, LBcurrentFolder, LBfolderStatus, LBspeed, LBtimeRemaining, LBcurrentFile, LBtaskStatus;
        MyButton BTNicon, BTNinfo, BTNpause;
        MyImage IMGspeedGraph;
        MyProgressBar PBsizeProgress, PBfileProgress, PBfolderProgress;
        private void SetBindings()
        {
            this.SetBinding(FolderUploadBar.MarginProperty, "Margin");
            BTNicon.SetBinding(MyButton.TextProperty, "FoldAndIcon");
            BTNicon.SetBinding(MyButton.CommandProperty, "FoldClicked");
            BTNicon.SetBinding(MyButton.IsEnabledProperty, "IsFoldEnabled");
            LBname.SetBinding(MyLabel.TextProperty, "Name");
            LBtaskStatus.SetBinding(MyLabel.TextProperty, "TaskStatus");
            LBcurrentFile.SetBinding(MyLabel.TextProperty, "CurrentFile", BindingMode.Default, new FolderUploadBarViewModel.FileTextValueConverter());
            LBcurrentFolder.SetBinding(MyLabel.TextProperty, "CurrentFolder", BindingMode.Default, new FolderUploadBarViewModel.FolderTextValueConverter());
            LBcurrentSize.SetBinding(MyLabel.TextProperty, "CurrentSize", BindingMode.Default, new FolderUploadBarViewModel.SizeTextValueConverter());
            LBtotalSize.SetBinding(MyLabel.TextProperty, "TotalSize", BindingMode.Default, new FolderUploadBarViewModel.SizeTextValueConverter());
            LBfileStatus.SetBinding(MyLabel.TextProperty, "FileStatus");
            LBfolderStatus.SetBinding(MyLabel.TextProperty, "FolderStatus");
            LBpercentage.SetBinding(MyLabel.TextProperty, "Progress", BindingMode.Default, new FolderUploadBarViewModel.ProgressTextValueConverter());
            LBspeed.SetBinding(MyLabel.TextProperty, "Speed");
            LBtimeRemaining.SetBinding(MyLabel.TextProperty, "TimeRemaining");
            BTNinfo.SetBinding(MyButton.TextProperty, "Info");
            BTNinfo.SetBinding(MyButton.CommandProperty, "InfoClicked");
            BTNinfo.SetBinding(MyButton.IsEnabledProperty, "InfoEnabled");
            //PBprogress.SetBinding(MyProgressBar.ProgressProperty, "Progress");
            PBfileProgress.SetBinding(MyProgressBar.ProgressProperty, "FileProgress");
            PBfolderProgress.SetBinding(MyProgressBar.ProgressProperty, "FolderProgress");
            PBsizeProgress.SetBinding(MyProgressBar.ProgressProperty, "Progress");
            IMGspeedGraph.SetBinding(MyImage.SourceProperty, "SpeedGraph");
            BTNpause.SetBinding(MyButton.TextProperty, "PauseButtonText");
            BTNpause.SetBinding(MyButton.CommandProperty, "PauseClicked");
            BTNpause.SetBinding(MyButton.IsEnabledProperty, "PauseButtonEnabled");
        }
        private void ArrangeViews()
        {
            this.RowSpacing = 0.5;
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75, GridUnitType.Absolute) });//icon
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//name
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//percentage, speed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//uploaded, time passed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//total, time remaining
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//info
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Absolute) });//pause button
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Absolute) });
            {
                //this.AddChildrenAndSetSpan(PBprogress, 3, 0, 2, 1);
                this.AddChildrenAndSetSpan(PBsizeProgress, 3, 0, 3, 1);
                this.AddChildrenAndSetSpan(IMGspeedGraph, 3, 0, 3, 1);
                this.AddChildrenAndSetSpan(PBfileProgress, 3, 1, 3, 1);
                this.AddChildrenAndSetSpan(PBfolderProgress, 3, 2, 3, 1);

                this.AddChildrenAndFillHeight(BTNicon, 0);
                this.AddChildrenAndFillHeight(LBname, 1);

                this.Children.Add(LBpercentage, 2, 0);
                this.Children.Add(LBspeed, 2, 1);
                this.AddChildrenAndSetSpan(LBtaskStatus, 2, 2, 2, 1);

                this.Children.Add(LBcurrentSize, 3, 0);
                this.Children.Add(LBtimeRemaining, 4, 0);
                this.Children.Add(LBtotalSize, 5, 0);

                this.AddChildrenAndSetSpan(LBcurrentFile, 3, 1, 2, 1);
                this.Children.Add(LBfileStatus, 5, 1);

                this.AddChildrenAndSetSpan(LBcurrentFolder, 3, 2, 2, 1);
                this.Children.Add(LBfolderStatus, 5, 2);

                this.AddChildrenAndFillHeight(BTNinfo, 6);
                this.AddChildrenAndFillHeight(BTNpause, 7);
            }
        }
        private void InitializeViews()
        {
            {
                BTNicon = new MyButton();
                LBname = new MyLabel();
                LBcurrentSize = new MyLabel();
                LBtotalSize = new MyLabel();
                LBfileStatus = new MyLabel();
                LBcurrentFolder = new MyLabel();
                LBfolderStatus = new MyLabel();
                LBpercentage = new MyLabel();
                LBspeed = new MyLabel();
                LBtimeRemaining = new MyLabel();
                LBcurrentFile = new MyLabel();
                LBtaskStatus = new MyLabel();
                BTNinfo = new MyButton();
                //PBprogress = new MyProgressBar();
                PBsizeProgress = new MyProgressBar();
                PBfileProgress = new MyProgressBar();
                PBfolderProgress = new MyProgressBar();
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
    class FolderUploadPage : MyContentPage
    {
        public FolderUploadPage()
        {
            this.Title = "Folder Upload";
            this.Content = new FolderUploadBarsList();
        }
    }
}
