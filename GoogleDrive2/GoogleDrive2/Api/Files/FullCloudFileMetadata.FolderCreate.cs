using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public partial class FolderCreate
        {
            static int RunningCount = 0, QueuedCount = 0, WaitingForMetadataCount = 0;
            public static event Libraries.Events.MyEventHandler<int> RunningCountChanged, QueuedCountChanged, WaitingForMetadataCountChanged;
            static void AddRunningCount(int value) { System.Threading.Interlocked.Add(ref RunningCount, value); RunningCountChanged?.Invoke(RunningCount); }
            static void AddQueuedCount(int value) { System.Threading.Interlocked.Add(ref QueuedCount, value); QueuedCountChanged?.Invoke(QueuedCount); }
            static void AddWaitingForMetadataCount(int value) { System.Threading.Interlocked.Add(ref WaitingForMetadataCount, value);WaitingForMetadataCountChanged?.Invoke(WaitingForMetadataCount); }
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
            public event Libraries.Events.MyEventHandler<string> FolderCreateCompleted;
            private void OnFolderCreateCompleted(string id)
            {
                if (id == null) this.LogError("id is null");
                MyLogger.Assert(id != null);
                GetCloudId = () => { return Task.FromResult(id); };
                OnCompleted();
                FolderCreateCompleted?.Invoke(id);
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
            Libraries.MySemaphore semaphoreNotCreateTwice = new Libraries.MySemaphore(1);
            static Libraries.MySemaphore semaphore = new Libraries.MySemaphore(MaxConcurrentFolderCreateOperation);
            Api.Files.FullCloudFileMetadata metadataForMainTask = null;
            protected override async Task PrepareBeforeStartAsync()
            {
                AddWaitingForMetadataCount(1);
                metadataForMainTask = await GetFolderMetadata();
                AddWaitingForMetadataCount(-1);
            }
            protected override async Task StartMainTaskAsync()
            {
                if (IsPausing || metadataForMainTask == null) return;
                await semaphoreNotCreateTwice.WaitAsync();
                AddRunningCount(1);
                try
                {
                    if (IsPausing) return;
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
                    semaphoreNotCreateTwice.Release();
                }
            }
        }
    }
}