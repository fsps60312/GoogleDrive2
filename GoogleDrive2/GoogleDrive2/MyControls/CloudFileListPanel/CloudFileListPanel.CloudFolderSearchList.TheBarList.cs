using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanel
    {
        public partial class CloudFolderSearchList
        {
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
                Dictionary<string, BarsListPanel.Treap<CloudFileListPanelViewModel.CloudFileItemBarViewModel>.TreapNodePrototype> TreapNodes=new Dictionary<string, BarsListPanel.Treap<CloudFileListPanelViewModel.CloudFileItemBarViewModel>.TreapNodePrototype>();
                void Initialize(string q, List<string> orderBy)
                {
                    OrderBy = orderBy;
                    Lister = new CloudFileListPanelViewModel.CloudFolderSearchListViewModel(q, orderBy);
                    Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
                    int desiredIdx = 0;
                    Lister.OperationStarted += delegate
                    {
                        desiredIdx = 0;
                        OperationStarted?.Invoke();
                    };
                    Lister.OperationEnded +=async delegate
                    {
                        await Libraries.MyTask.WhenAll(this.ToList().Select(async(f,idx) =>
                        {
                            if (f.UnderVerification)
                            {
                                await f.OnDisposedAsync(IsBarVisible(idx));
                            }
                        }));
                        OperationEnded?.Invoke();
                    };
                    Lister.ErrorOccurred += (msg) => { ErrorOccurred?.Invoke(msg); };
                    Lister.CloudFileListCleared += async () =>
                    {
                        CloudFileListCleared?.Invoke();
                        await semaphore.WaitAsync();
                        try
                        {
                            await Libraries.MyTask.WhenAll(this.ToList().Select((f) => { f.UnderVerification = true;return Task.CompletedTask; }));
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
                                UpdateItem(fileProperty, desiredIdx++);
                            }
                        }
                        finally { semaphore.Release(); }
                    };
                }
                private void UpdateItem(Api.Files.FullCloudFileMetadata fileProperty,int desiredIdx)
                {
                    if (TreapNodes.ContainsKey(fileProperty.id))
                    {
                        var u = TreapNodes[fileProperty.id];
                        u.data.UnderVerification = false;
                        u.data.Initialize(fileProperty);
                        var from = Treap.QueryPosition(u);
                        this.MoveItem(from, desiredIdx);
                    }
                    else
                    {
                        var newItem = new CloudFileListPanelViewModel.CloudFileItemBarViewModel(fileProperty);
                        newItem.Disposed += delegate { ItemRemoved?.Invoke(newItem); };
                        ItemAdded?.Invoke(newItem);
                        this.Insert(newItem,desiredIdx);
                    }
                }
                public TheBarList(string q, List<string> orderBy)
                {
                    Initialize(q, orderBy);
                    TreapNodeAdded += (o) =>
                    {
                        TreapNodes.Add(o.data.File.id, o);
                    };
                    TreapNodeRemoved += (o) =>
                    {
                        TreapNodes.Remove(o.data.File.id);
                    };
                }
            }
        }
    }
}
