using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanel
    {
        public partial class CloudFolderSearchList: MyContentView
        {
            public CloudFileListPanelViewModel.CloudFileItemBarViewModel FocusedItem = null;
            public event Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel> ItemClicked,ItemAdded,ItemRemoved, ItemToggled;
            public event Libraries.Events.MyEventHandler<bool> MultiSelectionToggled;
            event Libraries.Events.EmptyEventHandler RefreshRequested;
            public bool IsMultiSelectionToggled = false;
            public string Q { get; private set; }
            TheBarList BLmain;
            MyGrid GDmain;
            SearchListControlPanel GDctrl;
            MyScrollView SVctrl;
            public void Stop() { BLmain.Stop(); }
            public void Refresh()
            {
                RefreshRequested?.Invoke();
            }
            void ToggleTrashed(bool trashed)
            {
                BLmain.ChangeQ($"({Q}) and{(trashed ? "" : " not")} trashed");
                Refresh();
            }
            private void InitializeViews(string q, List<string> orderBy)
            {
                GDmain = new MyGrid();
                GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(50, Xamarin.Forms.GridUnitType.Absolute) });
                GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Star) });
                {
                    SVctrl = new MyScrollView { Orientation = ScrollOrientation.Vertical };
                    {
                        GDctrl = new SearchListControlPanel(this);
                        SVctrl.Content = GDctrl;
                    }
                    GDmain.Children.Add(SVctrl, 0, 0);
                }
                {
                    BLmain = new TheBarList($"({Q}) and not trashed", orderBy);
                    //var toggledEventHandler = new Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel>((f)=>
                    //{
                    //    this.ItemToggled?.Invoke(f);
                    //});
                    BLmain.ItemAdded += (item) =>
                    {
                        item.Clicked = new Command(() =>
                          {
                              ItemClicked?.Invoke(item);
                              GDctrl.LBtitle.Text = item.File.id;
                              if (FocusedItem != null) FocusedItem.Focused = false;
                              (FocusedItem = item).Focused = true;
                          });
                        //item.Toggled += toggledEventHandler;
                        ItemAdded?.Invoke(item);
                    };
                    BLmain.ItemRemoved += (item) =>
                    {
                        item.Clicked = null;
                        //item.Toggled -= toggledEventHandler;
                        ItemRemoved?.Invoke(item);
                    };
                    GDmain.Children.Add(BLmain, 0, 1);
                    //MyGrid.SetColumnSpan(BLmain, GDmain.ColumnDefinitions.Count);
                }
                this.Content = GDmain;
            }
            private void RegisterEvents()
            {
                GDctrl.BTNrefresh.RegisterEvents(this);
                this.MultiSelectionToggled += (t) => { IsMultiSelectionToggled = t; };
                var toggledEventHandler = new Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel>((f) =>
                {
                    ItemToggled?.Invoke(f);
                });
                BLmain.ItemAdded += (item) =>
                  {
                      item.Toggled += toggledEventHandler;
                      if (FocusedItem != null && item.File.id == FocusedItem.File.id)
                      {
                          FocusedItem.Focused = false;
                          (FocusedItem = item).Focused = true;
                      }
                  };
                BLmain.ItemRemoved += (item) =>
                  {
                      item.Toggled -= toggledEventHandler;
                  };
                bool bugsInXamarinFixed = false;// see: https://bugzilla.xamarin.com/show_bug.cgi?id=38770
                if (bugsInXamarinFixed)
                {
                    //var r = new Xamarin.Forms.PanGestureRecognizer();
                    ////double x = double.NaN, y = 0;
                    //bool dragging = false;
                    //r.PanUpdated += (sender, args) =>
                    //  {
                    //      //(this.Parent as Xamarin.Forms.Layout).LowerChild(this);
                    //      if(dragging)
                    //      {
                    //          if(args.StatusType == GestureStatus.Completed)
                    //          {
                    //              //LBtitle.HeightRequest /= 3;
                    //              //LBtitle.WidthRequest /= 3;
                    //              LBtitle.Scale = 1;
                    //              LBtitle.Opacity = 1;
                    //              //LBtitle.TranslationX += LBtitle.WidthRequest;
                    //              //LBtitle.TranslationY += LBtitle.HeightRequest;
                    //              dragging = false;
                    //          }
                    //      }
                    //      else
                    //      {
                    //          if(args.StatusType==GestureStatus.Running)
                    //          {
                    //              //LBtitle.TranslationX -= LBtitle.WidthRequest;
                    //              //LBtitle.TranslationY -= LBtitle.HeightRequest;
                    //              //LBtitle.HeightRequest *= 3;
                    //              //LBtitle.WidthRequest *= 3;
                    //              //var b = new MyBoxView();
                    //              (LBtitle.Parent as Layout).RaiseChild(LBtitle);//bug of Xamarin, will fix soon
                    //              //GDmain.Children.Remove(LBtitle);
                    //              //GDmain.Children.Add(LBtitle);
                    //              //(this.Parent as Layout).RaiseChild(this);//bug of Xamarin, will fix soon
                    //              LBtitle.Scale = 3;
                    //              LBtitle.Opacity = 0.5;
                    //              dragging = true;
                    //          }
                    //      }
                    //      this.TranslationX = args.TotalX;
                    //      this.TranslationY = args.TotalY;
                    //      //this.ForceLayout();
                    //      //this.UpdateChildrenLayout();
                    //  };
                    //GDctrl.LBtitle.GestureRecognizers.Add(r);
                }
                //this.SizeChanged += delegate { ChangeWidth(); };
            }
            public CloudFolderSearchList(string q, List<string> orderBy)
            {
                Q = q;
                InitializeViews(q, orderBy);
                RegisterEvents();
                Refresh();
            }
            class RefreshButton:MyButton
            {
                EventHandler action1, action2;
                public RefreshButton()
                {
                    this.Text = Constants.Icons.Refresh;
                }
                public void RegisterEvents(CloudFolderSearchList parent)
                {
                    var searchList = parent.BLmain;
                    parent.RefreshRequested += async delegate { await searchList.RefreshAsync(); };
                    action1 = new EventHandler(async(sender, args) =>
                    {
                        await searchList.RefreshAsync();
                    });
                    this.Clicked += action1;
                    bool isRunning = false;
                    long folders = 0, files = 0;
                    searchList.OperationStarted += delegate { this.IsEnabled = false; isRunning = true; UpdateText(isRunning, folders, files); };
                    searchList.OperationEnded += delegate { this.IsEnabled = true; isRunning = false; UpdateText(isRunning, folders, files); };
                    searchList.CloudFileListCleared += delegate
                    {
                        folders = files = 0;
                        UpdateText(isRunning, folders, files);
                    };
                    searchList.CloudFilesAdded += (fs) =>
                    {
                        foreach (var f in fs)
                        {
                            if (f.mimeType == Constants.FolderMimeType) folders++;
                            else files++;
                        }
                        UpdateText(isRunning, folders, files);
                    };
                    searchList.ErrorOccurred += (errorText)=>
                    {
                        this.IsEnabled = true; isRunning = false;
                        UpdateText(null, folders, files);
                        this.Clicked -= action1;
                        action2 = new EventHandler(async (sender, args) =>
                          {
                              await MyLogger.Alert(errorText);
                              this.Clicked -= action2;
                              this.Clicked += action1;
                              UpdateText(isRunning, folders, files);
                          });
                        this.Clicked += action2;
                    };
                }
                void UpdateText(bool? isRunning, long folderCount, long fileCount)
                {
                    var s1 = isRunning.HasValue ? (isRunning.Value ? Constants.Icons.Hourglass : Constants.Icons.Refresh) : Constants.Icons.Warning;
                    var s2 = (folderCount == 0 ? "" : $" | {Constants.Icons.Folder}{folderCount}") + (fileCount == 0 ? "" : $" | {Constants.Icons.File}{fileCount}");
                    if (string.IsNullOrEmpty(s2)) s2 = $" | {Constants.Icons.Mushroom}0";
                    this.Text = s1 + s2;
                }
            }
            class TheBarList : BarsListPanel.BarsListPanel<CloudFileItemBar, CloudFileListPanelViewModel.CloudFileItemBarViewModel>
            {
                public event Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel> ItemAdded,ItemRemoved;
                public event Libraries.Events.EmptyEventHandler OperationStarted, OperationEnded, CloudFileListCleared;
                public event Libraries.Events.MyEventHandler<List<Api.Files.FullCloudFileMetadata>> CloudFilesAdded;
                public event Libraries.Events.MyEventHandler<string> ErrorOccurred;
                CloudFileListPanelViewModel.CloudFolderSearchListViewModel Lister;
                public void Stop() { Lister.Stop(); }
                public async Task RefreshAsync() { await Lister.StartAsync(true); }
                public void ChangeQ(string q)
                {
                    Lister.Stop();
                    Initialize(q, OrderBy);
                }
                List<string> OrderBy;
                void Initialize(string q, List<string> orderBy)
                {
                    OrderBy = orderBy;
                    Lister = new CloudFileListPanelViewModel.CloudFolderSearchListViewModel(q, orderBy);
                    Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
                    Lister.OperationStarted += delegate { OperationStarted?.Invoke(); };
                    Lister.OperationEnded += delegate { OperationEnded?.Invoke(); };
                    Lister.ErrorOccurred += (msg) => { ErrorOccurred?.Invoke(msg); };
                    Lister.CloudFileListCleared += async () =>
                    {
                        CloudFileListCleared?.Invoke();
                        await semaphore.WaitAsync();
                        try
                        {
                            await this.ClearAsync();
                        }
                        finally { semaphore.Release(); }
                    };
                    Lister.CloudFilesAdded += async (files) =>
                    {
                        CloudFilesAdded?.Invoke(files);
                        await semaphore.WaitAsync();
                        try
                        {
                            foreach (var fileProperty in files)
                            {
                                var newItem = new CloudFileListPanelViewModel.CloudFileItemBarViewModel(fileProperty);
                                newItem.Disposed += delegate { ItemRemoved?.Invoke(newItem); };
                                ItemAdded?.Invoke(newItem);
                                this.PushBack(newItem);
                            }
                        }
                        finally { semaphore.Release(); }
                    };
                }
                public TheBarList(string q, List<string> orderBy)
                {
                    Initialize(q, orderBy);
                }
            }
        }
        public class ChainedSearchList : CloudFolderSearchList
        {
            public ChainedSearchList leftChild = null, rightChild = null;
            public event Libraries.Events.MyEventHandler<ChainedSearchList> ListAdded, ListRemoved;
            public bool IsRightChildAutoGenerated { get; private set; } = false;
            private void RemoveRightChild()
            {
                if (rightChild == null) return;
                var removedChild = rightChild;
                IsRightChildAutoGenerated = removedChild.IsRightChildAutoGenerated;
                removedChild.IsRightChildAutoGenerated = false;
                rightChild = removedChild.rightChild;
                if (rightChild != null) rightChild.leftChild = this;
                removedChild.leftChild = removedChild.rightChild = null;
                ListRemoved?.Invoke(removedChild);
            }
            private void InsertRightChild(bool isRightChildAutoGenerated, ChainedSearchList newList)
            {
                //MyLogger.Assert(!IsRightChildAutoGenerated);
                this.IsRightChildAutoGenerated = isRightChildAutoGenerated;
                if (rightChild != null) rightChild.leftChild = newList;
                newList.rightChild = rightChild;
                rightChild = newList;
                newList.leftChild = this;
            }
            public ChainedSearchList(string q, List<string> orderBy, Func<Api.Files.FullCloudFileMetadata, Task<Tuple<string, List<string>>>> parameterGenerator) : base(q, orderBy)
            {
                this.ListRemoved += (l) => { l.Stop(); };
                this.ItemClicked += async (f) =>
                {
                    while (IsRightChildAutoGenerated) RemoveRightChild();
                    if (f.File.mimeType != Constants.FolderMimeType) return;
                    var ps = await parameterGenerator(f.File);
                    var newList = new ChainedSearchList(ps.Item1, ps.Item2, parameterGenerator);
                    InsertRightChild(true, newList);
                    ListAdded?.Invoke(newList);
                };
            }
        }
    }
}
