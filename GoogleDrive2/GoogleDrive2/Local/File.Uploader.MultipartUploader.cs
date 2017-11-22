using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public class MultipartUploader : Uploader
            {
                protected override async Task StartUploadAsync(Api.Files.FullCloudFileMetadata metadata)
                {
                    MyLogger.Assert(BytesUploaded == 0 && TotalSize <= int.MaxValue);
                    if (ConfirmPauseSignal()) return;
                    var request = new Api.Files.MultipartUpload(metadata, await F.ReadBytesAsync((int)TotalSize));
                    F.CloseReadIfNot();
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            BytesUploaded = TotalSize;
                            OnUploadCompleted(ParseCloudId(await request.GetResponseTextAsync(response)));
                        }
                        else
                        {
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                        }
                    }
                }
                public MultipartUploader(File file) : base(file) { }
            }
        }
    }
}
