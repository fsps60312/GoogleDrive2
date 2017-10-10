﻿using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
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
                        OnUploadCompleted(f.id);
                    }
                    else OnErrorOccurred(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
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