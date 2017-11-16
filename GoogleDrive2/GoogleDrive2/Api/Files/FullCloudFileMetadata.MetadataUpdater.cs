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
            public Func<Task<string>> GetCloudId { get; private set; } = null;
            private void InitializeGetCloudIdTask()
            {
                Libraries.MySemaphore semaphore = new Libraries.MySemaphore(0);
                string resultId = null;
                GetCloudId = async () =>
                {
                    lock (GetCloudId)
                    {
                        Libraries.Events.MyEventHandler<string> folderCreateCompletedEventHandler = null;
                        folderCreateCompletedEventHandler = new Libraries.Events.MyEventHandler<string>((id) =>
                        {
                            this.FolderCreateCompleted -= folderCreateCompletedEventHandler;
                            resultId = id;
                            semaphore.Release();
                        });
                        FolderCreateCompleted += folderCreateCompletedEventHandler;

                        Libraries.Events.MyEventHandler<object> unstartedEventHandler = null;
                        unstartedEventHandler = new Libraries.Events.MyEventHandler<object>((sender) =>
                        {
                            if (!IsCompleted)
                            {
                                this.Unstarted -= unstartedEventHandler;
                                resultId = null;
                                semaphore.Release();
                            }
                        });
                        Unstarted += unstartedEventHandler;
                    }
                    await semaphore.WaitAsync();
                    return resultId;
                };
            }
        }
        public class Starrer : MetadataUpdater
        {
            public Starrer(string fileId, bool starred) : base(fileId, new FullCloudFileMetadata { starred = starred }) { }
        }
        public class Trasher : MetadataUpdater
        {
            public Trasher(string fileId, bool trashed) : base(fileId, new FullCloudFileMetadata { trashed = trashed }) { }
        }
        public class MetadataUpdater : Libraries.MyTask
        {
            string fileId;
            FullCloudFileMetadata metadata;
            protected override Task PrepareBeforeStartAsync()
            {
                return Task.CompletedTask;
            }
            protected override async Task StartMainTaskAsync()
            {
                var request = new UpdateMetadata(fileId, metadata);
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                        MyLogger.Assert(f.id == fileId);
                        OnCompleted();
                        return;
                    }
                    else
                    {
                        this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                        return;
                    }
                }
            }
            public MetadataUpdater(string fileId, FullCloudFileMetadata metadata)
            {
                this.fileId = fileId;
                this.metadata = metadata;
            }
        }
    }
}