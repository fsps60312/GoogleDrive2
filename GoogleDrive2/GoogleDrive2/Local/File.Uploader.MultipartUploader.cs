using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public class MultipartUploader : Uploader
            {
                protected override async Task<bool> StartUploadAsync(Api.Files.FullCloudFileMetadata metadata)
                {
                    MyLogger.Assert(BytesUploaded == 0 && TotalSize <= int.MaxValue);
                    if (CheckPause()) return false;
                    var request = new Api.Files.MultipartUpload(metadata, await F.ReadBytesAsync((int)TotalSize));
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
                public MultipartUploader(File file) : base(file) { }
            }
        }
    }
}
