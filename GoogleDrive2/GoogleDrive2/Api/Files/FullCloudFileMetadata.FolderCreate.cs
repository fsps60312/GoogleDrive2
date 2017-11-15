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
        public partial class FolderCreate : SimpleApiOperator
        {
            public const int MaxConcurrentFolderCreateOperation = 5;
            public event Libraries.Events.MyEventHandler<string> FolderCreateCompleted;
            private void OnFolderCreateCompleted(string id)
            {
                if (id == null) this.LogError("id is null");
                MyLogger.Assert(id != null);
                GetCloudId = () => { return Task.FromResult(id); };
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
            bool stopRequest = false;
            public void Stop()
            {
                stopRequest = true;
            }
            protected override async Task<bool> StartPrivateAsync()
            {
                stopRequest = false;
                AddWaitingForMetadataCount(1);
                var metadata = await GetFolderMetadata();
                AddWaitingForMetadataCount(-1);
                if (stopRequest || metadata == null) return false;
                try
                {
                    AddQueuedCount(1);
                    await semaphore.WaitAsync();
                    await semaphoreNotCreateTwice.WaitAsync();
                    AddQueuedCount(-1);
                    AddRunningCount(1);
                    if (stopRequest) return false;
                    var request = new MultipartUpload(metadata, new byte[0]);
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == HttpStatusCode.OK)
                        {
                            var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                            OnFolderCreateCompleted(f.id);
                            return true;
                        }
                        else
                        {
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return false;
                        }
                    }
                }
                catch (Exception error)
                {
                    this.LogError(error.ToString());
                    return false;
                }
                finally
                {
                    AddRunningCount(-1);
                    semaphoreNotCreateTwice.Release();
                    semaphore.Release();
                }
            }
        }
    }
}