﻿using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public class MultipartUploader : UploaderPrototype
            {
                protected override async Task<bool> StartUploadAsync()
                {
                    MyLogger.Assert(BytesUploaded == 0 && TotalSize <= int.MaxValue);
                    if (CheckPause()) return false;
                    var request = new Api.Files.MultipartUpload(FileMetadata, await F.ReadBytesAsync((int)TotalSize));
                    F.CloseReadIfNot();
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            BytesUploaded = TotalSize;
                            OnUploadCompleted(ParseCloudId(await request.GetResponseTextAsync(response)));
                            return true;
                        }
                        else
                        {
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return false;
                        }
                    }
                }
                public MultipartUploader(File file, Api.Files.FullCloudFileMetadata fileMetadata) : base(file, fileMetadata) { }
            }
        }
    }
}
