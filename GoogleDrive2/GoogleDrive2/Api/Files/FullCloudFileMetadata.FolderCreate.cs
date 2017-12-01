using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public partial class FolderCreate //be sure to call FolderCreateCompleted(id) or Completed(false) exactly once so that GetCloudId would work
        {
            public event Libraries.Events.MyEventHandler<string> FolderCreateCompleted;
            private string ResultCloudId = null;
            public async Task<string> GetCloudId(System.Threading.CancellationToken cancellationToken)
            {
                Libraries.MySemaphore semaphore;
                lock (syncRootChangeRunningState)
                {
                    if (IsCompleted) return ResultCloudId;
                    //if (!IsRunning) return ResultCloudId;//This line cause file.uploader behave like start & pause immediately
                    semaphore = new Libraries.MySemaphore(0);
                    Libraries.Events.MyEventHandler<object> unstartedEventHandler = null;
                    unstartedEventHandler = new Libraries.Events.MyEventHandler<object>((sender) =>
                    {
                        lock (syncRootChangeRunningState)
                        {
                            semaphore.Release();
                            Unstarted -= unstartedEventHandler;
                        }
                    });
                    Unstarted += unstartedEventHandler;
                }
                await semaphore.WaitAsync(cancellationToken);//No matter if the semaphore is cancelled
                return ResultCloudId;
            }
            private void OnFolderCreateCompleted(string id)
            {
                lock (syncRootChangeRunningState)
                {
                    if (id == null) this.LogError("id is null");
                    MyLogger.Assert(id != null);
                    ResultCloudId = id;
                    OnCompleted();
                    FolderCreateCompleted?.Invoke(id);
                }
            }
            private void InitializeGetCloudIdTask()
            {
                //Do nothing
            }
        }
        public partial class FolderCreate
        {
            static int RunningCount = 0, QueuedCount = 0, WaitingForMetadataCount = 0;
            public static event Libraries.Events.MyEventHandler<int> RunningCountChanged, QueuedCountChanged, WaitingForMetadataCountChanged;
            static void AddRunningCount(int value) { System.Threading.Interlocked.Add(ref RunningCount, value); RunningCountChanged?.Invoke(RunningCount); }
            static void AddQueuedCount(int value) { System.Threading.Interlocked.Add(ref QueuedCount, value); QueuedCountChanged?.Invoke(QueuedCount); }
            static void AddWaitingForMetadataCount(int value) { System.Threading.Interlocked.Add(ref WaitingForMetadataCount, value); WaitingForMetadataCountChanged?.Invoke(WaitingForMetadataCount); }
        }
        public partial class FolderCreate : Libraries.MyTask
        {
            public const int MaxConcurrentFolderCreateOperation = 5;
            static Libraries.MyTaskQueue folderCreateQueue = new Libraries.MyTaskQueue(MaxConcurrentFolderCreateOperation);
            public FolderCreate()
            {
                this.TaskQueue = folderCreateQueue;
                this.Queued += delegate { AddQueuedCount(1); };
                this.Unqueued += delegate { AddQueuedCount(-1); };
                InitializeGetCloudIdTask();
            }
            protected Func<Task<FullCloudFileMetadata>> GetFolderMetadata = ()
                => Task.FromResult(new FullCloudFileMetadata { mimeType = Constants.FolderMimeType });
            public void SetFolderMetadata(Func<FullCloudFileMetadata, Task<FullCloudFileMetadata>> func)
            {
                var preFunc = GetFolderMetadata;
                GetFolderMetadata = new Func<Task<FullCloudFileMetadata>>(async () =>
                {
                    var metadata = await preFunc();
                    return metadata == null ? null : await func(metadata);
                });
            }
            Api.Files.FullCloudFileMetadata metadataForMainTask = null;
            protected override async Task PrepareBeforeStartAsync()
            {
                AddWaitingForMetadataCount(1);
                metadataForMainTask = await GetFolderMetadata();
                AddWaitingForMetadataCount(-1);
            }
            protected override async Task StartMainTaskAsync()
            {
                if (ConfirmPauseSignal() || metadataForMainTask == null) return;
                AddRunningCount(1);
                try
                {
                    if (ConfirmPauseSignal()) return;
                    var request = new MultipartUpload(metadataForMainTask, new byte[0]);
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == HttpStatusCode.OK)
                        {
                            var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                            OnFolderCreateCompleted(f.id);
                            return;
                        }
                        else
                        {
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return;
                        }
                    }
                }
                catch (Exception error)
                {
                    this.LogError(error.ToString());
                    return;
                }
                finally
                {
                    AddRunningCount(-1);
                }
            }
        }
    }
}