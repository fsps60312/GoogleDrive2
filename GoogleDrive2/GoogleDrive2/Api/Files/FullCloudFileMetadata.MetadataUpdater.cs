using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public class FolderCreate : SimpleApiOperator
        {
            public event Libraries.Events.MyEventHandler<string> FolderCreateCompleted;
            private void OnFolderCreateCompleted(string id)
            {
                FolderCreateCompleted?.Invoke(id);
                OnCompleted();
            }
            object metaData;
            public override async Task StartAsync()
            {
                var request = new MultipartUpload(metaData, new byte[0]);
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                        OnFolderCreateCompleted(f.id);
                    }
                    else this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                }
            }
            public FolderCreate(object metaData)
            {
                this.metaData = metaData;
                if ((metaData as FullCloudFileMetadata).mimeType != Constants.FolderMimeType)
                {
                    MyLogger.LogError($"Folder mimeType expected: {(metaData as FullCloudFileMetadata).mimeType}");
                }
            }
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
                        OnCompleted();
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