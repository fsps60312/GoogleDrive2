using System;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanel
    {
        public partial class CloudFolderSearchList
        {
            class SearchListControlPanel : MyGrid
            {
                MyButton BTNselectAll,BTNshowInfo,BTNuploadFile,BTNtrash,BTNstar,BTNnewFolder;
                public RefreshButton BTNrefresh;
                public MyLabel LBtitle;
                MySwitch SWmultiSelectEnabled,SWtrash;
                new CloudFolderSearchList Parent;
                volatile int SelectedFileCount = 0, SelectedFolderCount = 0;
                private void ArrangeViews()
                {
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Absolute) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    {
                        this.Children.Add(BTNrefresh, 0, 0);
                        this.Children.Add(LBtitle, 1, 0);
                        this.Children.Add(BTNselectAll, 0, 1);
                        this.Children.Add(SWmultiSelectEnabled, 1, 1);
                        this.Children.Add(BTNshowInfo, 0, 2);
                        this.Children.Add(BTNuploadFile, 0, 3);
                        this.Children.Add(BTNtrash, 0, 4);
                        this.Children.Add(SWtrash, 1, 4);
                        this.Children.Add(BTNstar, 0, 5);
                        this.Children.Add(BTNnewFolder, 0, 6);
                    }
                }
                private void InitializaViews()
                {
                    BTNrefresh = new RefreshButton();
                    LBtitle = new MyLabel
                    {
                        BackgroundColor = Color.White,
                        WidthRequest = 150,
                        HeightRequest = 40
                    };
                    BTNselectAll = new MyButton { Text = Constants.Icons.CheckBox, IsEnabled = false };
                    SWmultiSelectEnabled = new MySwitch(null, null, false);
                    BTNshowInfo = new MyButton { Text = Constants.Icons.Info };
                    BTNuploadFile = new MyButton { Text = Constants.Icons.Upload };
                    BTNtrash = new MyButton { Text = Constants.Icons.TrashCan };
                    SWtrash = new MySwitch("Trash Can", "Folder", false);
                    BTNstar = new MyButton { Text = Constants.Icons.Star };
                    BTNnewFolder = new MyButton { Text = "+" + Constants.Icons.Folder };
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
                    var s1 = Constants.Icons.CheckBox;
                    var s2 = (SelectedFolderCount == 0 ? "" : $" | {Constants.Icons.Folder}{SelectedFolderCount}") + (SelectedFileCount == 0 ? "" : $" | {Constants.Icons.File}{SelectedFileCount}");
                    if (string.IsNullOrEmpty(s2)) s2 = $" | {Constants.Icons.Mushroom}0";
                    BTNselectAll.Text = s1 + s2;
                }
                async Task UploadButtonClicked()
                {
                    if (this.Parent.ClickedItem == null)
                    {
                        await MyLogger.Alert($"{Constants.Icons.Info} No item selected");
                        return;
                    }
                    var cloud = this.Parent.ClickedItem.File;
                    if (cloud.mimeType != Constants.FolderMimeType)
                    {
                        await MyLogger.Alert($"{Constants.Icons.Info} Item of Folder type expected");
                        return;
                    }
                    var file = await Local.File.OpenSingleFileAsync();
                    if (file == null) return;
                    //await MyLogger.Alert(file.MimeType);
                    var uploader = file.GetUploader();
                    uploader.FileMetadata.parents = new List<string> { cloud.id };
                    uploader.ErrorOccurred += async (msg) =>
                    {
                        await MyLogger.Alert($"Error:\r\n{msg}");
                    };
                    uploader.UploadCompleted += async (msg) =>
                    {
                        await MyLogger.Alert($"Completed:\r\n{msg}");
                    };
                    await uploader.StartAsync(true);
                    //await MyLogger.Alert("Completed");
                }
                async Task TrashButtonClicked()
                {
                    if (this.Parent.ClickedItem == null)
                    {
                        await MyLogger.Alert($"{Constants.Icons.Info} No item selected");
                        return;
                    }
                    if (Parent.IsMultiSelectionToggled)
                    {
                        await Task.WhenAll(Parent.ToggledItems.Select(async (f) =>
                        {
                            var trasher = f.File.GetTrasher(!f.File.trashed.Value);
                            trasher.ErrorOccurred += async (msg) => { await MyLogger.Alert($"Failed: {msg}"); };
                            await trasher.StartAsync();
                        }));
                    }
                    else
                    {
                        var cloud = this.Parent.ClickedItem.File;
                        var trasher = cloud.GetTrasher(!cloud.trashed.Value);
                        trasher.ErrorOccurred += async (msg) => { await MyLogger.Alert($"Failed: {msg}"); };
                        await trasher.StartAsync();
                    }
                    Parent.Refresh();
                }
                async Task StarButtonClicked()
                {
                    if (this.Parent.ClickedItem == null)
                    {
                        await MyLogger.Alert($"{Constants.Icons.Info} No item selected");
                        return;
                    }
                    if (Parent.IsMultiSelectionToggled)
                    {
                        await Task.WhenAll(Parent.ToggledItems.Select(async (f) =>
                        {
                            var starrer = f.File.GetStarrer(!f.File.starred.Value);
                            starrer.ErrorOccurred += async (msg) => { await MyLogger.Alert($"Failed: {msg}"); };
                            await starrer.StartAsync();
                        }));
                    }
                    else
                    {
                        var cloud = this.Parent.ClickedItem.File;
                        var starrer = cloud.GetStarrer(!cloud.starred.Value);
                        starrer.ErrorOccurred += async (msg) => { await MyLogger.Alert($"Failed: {msg}"); };
                        await starrer.StartAsync();
                    }
                    Parent.Refresh();
                }
                async Task CreateFolder()
                {
                    if(Parent.ClickedItem==null)
                    {
                        await MyLogger.Alert($"{Constants.Icons.Info} No item selected");
                        return;
                    }
                    var cloud = Parent.ClickedItem.File;
                    if(cloud.mimeType!=Constants.FolderMimeType)
                    {
                        var badChoice = "I still want to try";
                        var response=await MyLogger.ActionSheet(Constants.Icons.Warning,"Please select a Folder or Google Drive will hate you",new List<string> {"OK", badChoice});
                        if (response.Item2 != badChoice) return;
                    }
                    Tuple<string, string> name;
                    while (true)
                    {
                        name = await MyLogger.ActionSheet("Type your Folder Name", "New Folder", new List<string> { "OK", "Cancel" });
                        if (name.Item2 == "OK") break;
                        else
                        {
                            if ((await MyLogger.ActionSheet(Constants.Icons.Info, "Folder creation is about to be canceled. Are you sure?", new List<string> { "Yes", "No" })).Item2 == "Yes")
                            {
                                return;
                            }
                        }
                    }
                    var creator = cloud.GetFolderCreater(name.Item1);
                    creator.ErrorOccurred += async (msg) => { await MyLogger.Alert($"Failed: {msg}"); };
                    await creator.StartAsync();
                }
                private void RegisterEvents()
                {
                    Parent.ItemClicked += (f)=>
                      {
                          {
                              var s1 = f.File.mimeType == Constants.FolderMimeType ? Constants.Icons.Folder : Constants.Icons.File;
                              const string s2 = Constants.Icons.TrashCan;
                              if (f.File.trashed.Value) BTNtrash.Text = $"{s2}→{s1}";
                              else BTNtrash.Text = $"{s1}→{s2}";
                          }
                          {
                              if (f.File.starred.Value) BTNstar.BackgroundColor = Color.DodgerBlue;
                              else BTNstar.BackgroundColor = Color.Default;
                          }
                      };
                    BTNnewFolder.Clicked += async delegate
                      {
                          BTNnewFolder.IsEnabled = false;
                          var text = BTNnewFolder.Text;
                          BTNnewFolder.Text += Constants.Icons.Hourglass;
                          try { await CreateFolder(); }
                          finally { BTNnewFolder.Text = text; BTNnewFolder.IsEnabled = true; }
                      };
                    BTNstar.Clicked += async delegate
                      {
                          BTNstar.IsEnabled = false;
                          var text = BTNstar.Text;
                          BTNstar.Text += Constants.Icons.Hourglass;
                          try { await StarButtonClicked(); }
                          finally { BTNstar.Text = text; BTNstar.IsEnabled = true; }
                      };
                    SWtrash.Toggled += (sender,args)=>
                      {
                          Parent.ToggleTrashed(args.Value);
                      };
                    BTNtrash.Clicked += async delegate
                      {
                          BTNtrash.IsEnabled = false;
                          var text = BTNtrash.Text;
                          BTNtrash.Text += Constants.Icons.Hourglass;
                          try { await TrashButtonClicked(); }
                          finally { BTNtrash.IsEnabled = true; BTNtrash.Text = text; }
                      };
                    BTNuploadFile.Clicked += async delegate
                      {
                          BTNuploadFile.IsEnabled = false;
                          try { await UploadButtonClicked(); }
                          finally { BTNuploadFile.IsEnabled = true; }
                      };
                    BTNshowInfo.Clicked += async delegate
                      {
                          if(Parent.ClickedItem==null)
                          {
                              await MyLogger.Alert("ℹ No item selected");
                              return;
                          }
                          var file = Parent.ClickedItem.File;
                          await MyLogger.Alert(Libraries.MySerializer.SerializeFields(file));
                      };
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
                    Parent = parent;
                    InitializaViews();
                    ArrangeViews();
                    RegisterEvents();
                    UpdateText();
                }
            }
        }
    }
}
