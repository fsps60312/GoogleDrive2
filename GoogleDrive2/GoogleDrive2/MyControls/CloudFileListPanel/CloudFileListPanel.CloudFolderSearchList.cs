using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Linq;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanel
    {
        public class CloudFolderSearchList: MyContentView
        {
            public CloudFileListPanelViewModel.CloudFileItemBarViewModel FocusedItem = null;
            public event Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel> ItemClicked,ItemAdded,ItemRemoved, ItemToggled;
            public event Libraries.Events.MyEventHandler<bool> MultiSelectionToggled;
            public bool IsMultiSelectionToggled = false;
            TheBarList BLmain;
            MyGrid GDmain;
            SearchListControlPanel GDctrl;
            MyScrollView SVctrl;
            public void Stop() { BLmain.Stop(); }
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
                    BLmain = new TheBarList(q, orderBy);
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
                GDctrl.BTNrefresh.RegisterEvents(BLmain.Lister);
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
                InitializeViews(q, orderBy);
                RegisterEvents();
                new Action(async () => { await BLmain.Lister.StartAsync(true); })();
            }
            class SearchListControlPanel : MyGrid
            {
                MyButton BTNselectAll;
                public RefreshButton BTNrefresh;
                public MyLabel LBtitle;
                MySwitch SWmultiSelectEnabled;
                new CloudFolderSearchList Parent;
                volatile int SelectedFileCount = 0, SelectedFolderCount = 0;
                private void InitializaViews(CloudFolderSearchList parent)
                {
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Absolute) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    {
                        BTNrefresh = new RefreshButton();
                        this.Children.Add(BTNrefresh, 0, 0);
                    }
                    {
                        LBtitle = new MyLabel { BackgroundColor = Color.White };
                        LBtitle.WidthRequest = 150;
                        LBtitle.HeightRequest = 40;
                        this.Children.Add(LBtitle, 1, 0);
                    }
                    {
                        BTNselectAll = new MyButton { Text = "☑", IsEnabled = false };
                        this.Children.Add(BTNselectAll, 0, 1);
                        //MyGrid.SetColumnSpan(BTNselectAll, this.ColumnDefinitions.Count);
                    }
                    {
                        SWmultiSelectEnabled = new MySwitch(null,null,false);
                        this.Children.Add(SWmultiSelectEnabled, 1, 1);
                    }
                    Parent = parent;
                }
                private bool SelectAllState = false;
                private async Task Select(bool all)
                {
                    await Task.WhenAll((this.Parent as CloudFolderSearchList).BLmain.ToList().Select((o) =>
                     {
                         o.IsToggled = all;
                         return Task.CompletedTask;
                     }));
                    if (all)
                    {
                        BTNselectAll.BackgroundColor = Color.DodgerBlue;
                    }
                    else
                    {
                        BTNselectAll.BackgroundColor = Color.Default;
                    }
                    SelectAllState = all;
                }
                void UpdateText()
                {
                    var s1 = "☑";
                    var s2 = (SelectedFolderCount == 0 ? "" : $" | 📁{SelectedFolderCount}") + (SelectedFileCount == 0 ? "" : $" | 📄{SelectedFileCount}");
                    if (string.IsNullOrEmpty(s2)) s2 = " | 🍄0";
                    BTNselectAll.Text = s1 + s2;
                }
                private void RegisterEvents()
                {
                    {
                        var toggledEventHandler = new Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel>((f) =>
                        {
                            var v = f.IsToggled ? 1 : -1;
                            if (f.File.mimeType == Constants.FolderMimeType) SelectedFolderCount += v;
                            else SelectedFileCount += v;
                            UpdateText();
                        });
                        Libraries.Events.MyEventHandler<object> disposedEventHandler=null;
                        disposedEventHandler = new Libraries.Events.MyEventHandler<object>((fo) =>
                          {
                              var f = fo as CloudFileListPanelViewModel.CloudFileItemBarViewModel;
                              f.IsToggled = false;
                              f.Toggled -= toggledEventHandler;
                              f.Disposed -= disposedEventHandler;
                          });
                        this.Parent.ItemAdded += (item) =>
                        {
                            item.Toggled += toggledEventHandler;
                            item.Disposed += delegate { item.IsToggled = false; };
                        };
                    }
                    {
                        var eventHandler = new Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel>((f) =>
                          {
                              f.IsToggled ^= true;
                          });
                        SWmultiSelectEnabled.Toggled += async delegate
                          {
                              var toggled = BTNselectAll.IsEnabled = SWmultiSelectEnabled.IsToggled;
                              Parent.MultiSelectionToggled?.Invoke(toggled);
                              if (toggled)
                              {
                                  (this.Parent as CloudFolderSearchList).ItemClicked += eventHandler;
                              }
                              else
                              {
                                  (this.Parent as CloudFolderSearchList).ItemClicked -= eventHandler;
                                  await Select(false);
                              }
                          };
                    }
                    {
                        BTNselectAll.Clicked += async delegate
                           {
                               BTNselectAll.IsEnabled = false;
                               SelectAllState ^= true;
                               await Select(SelectAllState);
                               BTNselectAll.IsEnabled = true;
                           };
                    }
                }
                public SearchListControlPanel(CloudFolderSearchList parent)
                {
                    InitializaViews(parent);
                    RegisterEvents();
                    UpdateText();
                }
            }
            class RefreshButton:MyButton
            {
                EventHandler action1, action2;
                public RefreshButton()
                {
                    this.Text = "↻";
                }
                public void RegisterEvents(CloudFileListPanelViewModel.CloudFolderSearchListViewModel searchList)
                {
                    action1 = new EventHandler(async(sender, args) =>
                    {
                        await searchList.StartAsync(true);
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
                    var s1 = isRunning.HasValue ? (isRunning.Value ? "⏳" : "↻") : "⚠";
                    var s2 = (folderCount == 0 ? "" : $" | 📁{folderCount}") + (fileCount == 0 ? "" : $" | 📄{fileCount}");
                    if (string.IsNullOrEmpty(s2)) s2 = " | 🍄0";
                    this.Text = s1 + s2;
                }
            }
            class TheBarList : BarsListPanel.BarsListPanel<CloudFileItemBar, CloudFileListPanelViewModel.CloudFileItemBarViewModel>
            {
                public event Libraries.Events.MyEventHandler<CloudFileListPanelViewModel.CloudFileItemBarViewModel> ItemAdded,ItemRemoved;
                public CloudFileListPanelViewModel.CloudFolderSearchListViewModel Lister;
                public void Stop()
                {
                    Lister.Stop();
                }
                public TheBarList(string q, List<string> orderBy)
                {
                    Lister = new CloudFileListPanelViewModel.CloudFolderSearchListViewModel(q, orderBy);
                    Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
                    Lister.CloudFileListCleared += async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            await this.ClearAsync();
                        }
                        finally { semaphore.Release(); }
                    };
                    Lister.CloudFilesAdded += async (files) =>
                     {
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
            public ChainedSearchList(string q, List<string> orderBy, Func<Api.Files.FullList.FullProperties, Task<Tuple<string, List<string>>>> parameterGenerator) : base(q, orderBy)
            {
                this.ListRemoved += (l) => { l.Stop(); };
                this.ItemClicked += async (f) =>
                {
                    var ps = await parameterGenerator(f.File);
                    var newList = new ChainedSearchList(ps.Item1, ps.Item2, parameterGenerator);
                    while (IsRightChildAutoGenerated) RemoveRightChild();
                    InsertRightChild(true, newList);
                    ListAdded?.Invoke(newList);
                };
            }
        }
    }
}
