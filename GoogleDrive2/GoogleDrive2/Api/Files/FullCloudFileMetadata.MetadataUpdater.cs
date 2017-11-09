using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public partial class FolderCreate : SimpleApiOperator
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
            private bool OnFolderCreateCompleted(string id)
            {
                if (id == null)
                {
                    this.LogError("id is null");
                    return false;
                }
                GetCloudId = () => { return Task.FromResult(id); };
                FolderCreateCompleted?.Invoke(id);
                return true;
            }
            protected Func<Task<FullCloudFileMetadata>> GetFolderMetadata = ()
                => Task.FromResult(new FullCloudFileMetadata { mimeType = Constants.FolderMimeType });
            public void SetFolderMetadata(Func<FullCloudFileMetadata, Task<FullCloudFileMetadata>> func)
            {
                var preFunc = GetFolderMetadata;
                GetFolderMetadata = new Func<Task<FullCloudFileMetadata>>(async () =>
                  {
                      return await func(await preFunc());
                  });
            }
            Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
            bool stopRequest = false;
            public void Stop()
            {
                stopRequest = true;
            }
            protected override async Task<bool> StartPrivateAsync()
            {
                stopRequest = false;
                await semaphore.WaitAsync();
                if (stopRequest) return false;
                try
                {
                    var request = new MultipartUpload(await GetFolderMetadata(), new byte[0]);
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == HttpStatusCode.OK)
                        {
                            var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                            return OnFolderCreateCompleted(f.id);
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
                finally { semaphore.Release(); }
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