using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public partial class FolderCreate:SimpleApiOperator
        {
            const int MaxConcurrentCount = 10;
            public Func<Task<string>> GetCloudId { get; private set; } = null;
            public FolderCreate()
            {
                Libraries.MySemaphore semaphore = new Libraries.MySemaphore(0);
                string resultId = null;
                GetCloudId = async () =>
                {
                    lock (GetCloudId)
                    {
                        Libraries.Events.MyEventHandler<string> completedHandler = null;
                        completedHandler = new Libraries.Events.MyEventHandler<string>((id) =>
                        {
                            this.FolderCreateCompleted -= completedHandler;
                            resultId = id;
                            semaphore.Release();
                        });
                        FolderCreateCompleted += completedHandler;
                    }
                    await semaphore.WaitAsync();
                    MyLogger.Assert(resultId != null);
                    return resultId;
                };
            }
        }
        public partial class FolderCreate : SimpleApiOperator
        {
            public event Libraries.Events.MyEventHandler<string> FolderCreateCompleted;
            private void OnFolderCreateCompleted(string id)
            {
                GetCloudId = () => { return Task.FromResult(id); };
                FolderCreateCompleted?.Invoke(id);
                OnCompleted(id != null);
            }
            protected Func<Task<FullCloudFileMetadata>> GetFolderMetadata = ()
                => Task.FromResult(new FullCloudFileMetadata { mimeType = Constants.FolderMimeType });
            public void SetFolderMetadata(Func<FullCloudFileMetadata,Task<FullCloudFileMetadata>>func)
            {
                var preFunc = GetFolderMetadata;
                GetFolderMetadata = new Func<Task<FullCloudFileMetadata>>(async () =>
                  {
                      return await func(await preFunc());
                  });
            }
            static Libraries.MySemaphore semaphore = new Libraries.MySemaphore(MaxConcurrentCount);
            bool stopRequest = false;
            public void Stop()
            {
                stopRequest = true;
            }
            public override async Task StartAsync()
            {
                stopRequest = false;
                await semaphore.WaitAsync();
                try
                {
                    if (stopRequest)
                    {
                        OnCompleted(false);
                        return;
                    }
                    var request = new MultipartUpload(await GetFolderMetadata(), new byte[0]);
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == HttpStatusCode.OK)
                        {
                            var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                            OnFolderCreateCompleted(f.id);
                        }
                        else
                        {
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            OnCompleted(false);
                        }
                    }
                }
                catch (Exception error)
                {
                    this.LogError(error.ToString());
                    OnCompleted(false);
                }
                finally { semaphore.Release(); }
            }
            //public FolderCreate()
            //{
            //    //this.metaData = metaData;
            //    //if ((metaData as FullCloudFileMetadata).mimeType != Constants.FolderMimeType)
            //    //{
            //    //    MyLogger.LogError($"Folder mimeType expected: {(metaData as FullCloudFileMetadata).mimeType}");
            //    //}
            //}
        }
        public class Starrer:MetadataUpdater
        {
            public Starrer(string fileId, bool starred) : base(fileId, new FullCloudFileMetadata { starred = starred }) { }
        }
        public class Trasher : MetadataUpdater
        {
            public Trasher(string fileId,bool trashed) : base(fileId, new FullCloudFileMetadata { trashed = trashed }) { }
        }
        public class MetadataUpdater:Api.SimpleApiOperator
        {
            string fileId;
            FullCloudFileMetadata metadata;
            public override async Task StartAsync()
            {
                var request = new UpdateMetadata(fileId, metadata);
                using (var response = await request.GetHttpResponseAsync())
                {
                    if(response?.StatusCode==HttpStatusCode.OK)
                    {
                        var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                        MyLogger.Assert(f.id == fileId);
                        OnCompleted(true);
                    }
                    else this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                }
            }
            public MetadataUpdater(string fileId,FullCloudFileMetadata metadata)
            {
                this.fileId = fileId;
                this.metadata = metadata;
            }
        }
    }
}