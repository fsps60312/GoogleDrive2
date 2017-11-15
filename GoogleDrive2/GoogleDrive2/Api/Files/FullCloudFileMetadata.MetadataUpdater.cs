using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public partial class FolderCreate : SimpleApiOperator//be sure to call FolderCreateCompleted(id) or Completed(false) exactly once so that GetCloudId would work
        {
            public Func<Task<string>> GetCloudId { get; private set; } = null;
            public FolderCreate() 
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

                        Libraries.Events.MyEventHandler<object,bool> completedEventHandler = null;
                        completedEventHandler = new Libraries.Events.MyEventHandler<object,bool>((sender,success) =>
                        {
                            if (!success)
                            {
                                this.Completed -= completedEventHandler;
                                resultId = null;
                                semaphore.Release();
                            }
                        });
                        Completed += completedEventHandler;
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
        public class MetadataUpdater : Api.SimpleApiOperator
        {
            string fileId;
            FullCloudFileMetadata metadata;
            protected override async Task<bool> StartPrivateAsync()
            {
                var request = new UpdateMetadata(fileId, metadata);
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                        MyLogger.Assert(f.id == fileId);
                        return true;
                    }
                    else
                    {
                        this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                        return false;
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