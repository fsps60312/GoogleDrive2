using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public class Uploader:Api.AdvancedApiOperator
        {
            public const long MinChunkSize = 262144 + 1;
            File F;
            public bool Completed = false;
            long bytesUploaded = 0, totalSize = 0;
            public Api.Files.FullCloudFileMetadata FileMetadata = new Api.Files.FullCloudFileMetadata();
            async Task AssignFileMetadata()
            {
                totalSize = (long)await F.GetSizeAsync();
                FileMetadata.name = F.Name;
                //FileMetadata.size = (long)await F.GetSizeAsync();
                FileMetadata.createdTime = await F.GetTimeCreatedAsync();
                FileMetadata.modifiedTime = await F.GetTimeModifiedAsync();
                if (F.IsImageFile) FileMetadata.imageMediaMetadata = await F.GetImageMediaMetadataAsync();
                if (F.IsVideoFile) FileMetadata.videoMediaMetadata = await F.GetVideoMediaMetadataAsync();
            }
            async Task StartMultipartUploadAsync()
            {
                MyLogger.Assert(bytesUploaded == 0 && totalSize <= int.MaxValue);
                var request = new Api.Files.MultipartUpload(FileMetadata, await F.ReadBytesAsync((int)totalSize));
                F.CloseFileIfNot();
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        //bytesUploaded = FileMetadata.size.Value;
                        bytesUploaded = totalSize;
                        var f = JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(await request.GetResponseTextAsync(response));
                        OnUploadCompleted(f.id);
                    }
                    else OnErrorOccurred(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                }
            }
            string resumableUri = null;
            async Task<string> CreateResumableUploadAsync()
            {
                await MyLogger.Alert($"file size: {totalSize}");
                throw new System.NotImplementedException();
            }
            public override async Task StartAsync(bool startFromScratch)
            {
                if (Completed)
                {
                    MyLogger.LogError("Upload has already completed");
                    return;
                }
                if (startFromScratch)
                {
                    F.CloseFileIfNot();
                    await AssignFileMetadata();
                    bytesUploaded = 0;
                    if (totalSize <= MinChunkSize)
                    {
                        await StartMultipartUploadAsync();
                    }
                    else
                    {
                        resumableUri = await CreateResumableUploadAsync();
                    }
                }
            }
            public Uploader(File file)
            {
                F = file;
                UploadCompleted += (id)=> { Completed = true; };
            }
        }
    }
}
