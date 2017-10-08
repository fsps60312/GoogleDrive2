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
                MyButton BTNselectAll,BTNshowInfo;
                public RefreshButton BTNrefresh;
                public MyLabel LBtitle;
                MySwitch SWmultiSelectEnabled;
                new CloudFolderSearchList Parent;
                volatile int SelectedFileCount = 0, SelectedFolderCount = 0;
                private void InitializaViews(CloudFolderSearchList parent)
                {
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Absolute) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
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
                    {
                        BTNshowInfo = new MyButton { Text = "ℹ" };
                        this.Children.Add(BTNshowInfo, 0, 2);
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
                List<string> GetInfoAsStringList(object o)
                {
                    {
                        if (o is System.Collections.IEnumerable && !(o is string))
                        {
                            var ans = new List<string>();
                            int idx = 0;
                            foreach(var nxto in (System.Collections.IEnumerable)o)
                            {
                                ans.Add($"{($"[{idx++}]:").PadRight(15)} {nxto}");
                                if (nxto != null)
                                {
                                    foreach (var s in GetInfoAsStringList(nxto))
                                    {
                                        ans.Add($"    {s}");
                                    }
                                }
                            }
                            return ans;
                        }
                    }
                    {
                        var fs = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        List<string> ans = new List<string>();
                        foreach (var f in fs)
                        {
                            object nxto;
                            try
                            {
                                nxto = f.GetValue(o);
                            }
                            catch (Exception error)
                            {
                                nxto = error;
                            }
                            ans.Add($"{(f.Name + ":").PadRight(15)} {nxto}");
                            if (nxto != null)
                            {
                                foreach (var s in GetInfoAsStringList(nxto))
                                {
                                    ans.Add($"    {s}");
                                }
                            }
                        }
                        return ans;
                    }
                }
                string GetInfoAsString(object o)
                {
                    return string.Join("\r\n", GetInfoAsStringList(o));
                }
                private void RegisterEvents()
                {
                    BTNshowInfo.Clicked += async delegate
                      {
                          if(Parent.FocusedItem==null)
                          {
                              await MyLogger.Alert("ℹ No item selected");
                              return;
                          }
                          var file = Parent.FocusedItem.File;
                          await MyLogger.Alert(GetInfoAsString(file));
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
                    InitializaViews(parent);
                    RegisterEvents();
                    UpdateText();
                }
            }
        }
    }
}
