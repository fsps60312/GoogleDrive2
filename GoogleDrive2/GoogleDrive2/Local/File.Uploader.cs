using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public class Uploader:Api.AdvancedApiOperator
        {
            public const long MinChunkSize = 262144;// + 1;
            public const double DesiredProgressUpdateInterval = 0.5;
            File F;
            long bytesUploaded = 0, totalSize = 0;
            public Api.Files.FullCloudFileMetadata FileMetadata = new Api.Files.FullCloudFileMetadata();
            async Task AssignFileMetadata()
            {
                totalSize = (long)await F.GetSizeAsync();
                FileMetadata.name = F.Name;
                FileMetadata.createdTime = await F.GetTimeCreatedAsync();
                FileMetadata.modifiedTime = await F.GetTimeModifiedAsync();
            }
            string ParseCloudId(string content)
            {
                return JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(content).id;
            }
            async Task StartMultipartUploadAsync()
            {
                MyLogger.Assert(bytesUploaded == 0 && totalSize <= int.MaxValue);
                try
                {
                    var request = new Api.Files.MultipartUpload(FileMetadata, await F.ReadBytesAsync((int)totalSize));
                    F.CloseFileIfNot();
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            bytesUploaded = totalSize;
                            OnCompleted(ParseCloudId(await request.GetResponseTextAsync(response)));
                        }
                        else this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                    }
                }
                finally { F.CloseFileIfNot(); }
            }
            string resumableUri = null;
            async Task<bool> CreateResumableUploadAsync()
            {
                await MyLogger.Alert($"file size: {totalSize}");
                var request = new Api.Files.ResumableCreate(FileMetadata, totalSize,null);
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        resumableUri= response.Headers["location"];
                        return true;
                    }
                    else
                    {
                        this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                        return false;
                    }
                }
            }
            async Task<MyHttpResponse> DoSingleResumableUploadAsync(long position,long chunkSize)
            {
                await F.SeekAsync(position);
                var request = new Api.Files.ResumableUpload(resumableUri, totalSize, position, position + chunkSize - 1, await F.ReadBytesAsync((int)chunkSize));
                return await request.GetHttpResponseAsync();
            }
            async Task StartResumableUploadAsync(long position)
            {
                try
                {
                    long chunkCount = 1;
                    while (position < totalSize)
                    {
                        var time = DateTime.Now;
                        var realChunkSize = Math.Min(totalSize - position, chunkCount * MinChunkSize);
                        using (var response = await DoSingleResumableUploadAsync(position, realChunkSize))
                        {
                            var prePosition = position;
                            position += realChunkSize;
                            if ((DateTime.Now - time).TotalSeconds < DesiredProgressUpdateInterval) chunkCount += (chunkCount + 1) / 2;
                            else chunkCount = (chunkCount+1)/2;
                            switch (response?.StatusCode)
                            {
                                case System.Net.HttpStatusCode.OK:
                                case System.Net.HttpStatusCode.Created:
                                    OnCompleted(ParseCloudId(await response.GetResponseString()));
                                    break;
                                default:
                                    if ((int)response?.StatusCode == 308)
                                    {
                                        var newPosition = ParseRangeHeader(response);
                                        //MyLogger.Assert(newPosition == totalSize || newPosition % MinChunkSize == 0);
                                        if (newPosition != position)
                                        {
                                            this.LogError($"Server is expected to read {position-prePosition}({position}) bytes," +
                                                $" {newPosition-prePosition}({newPosition}) bytes actually\r\n" +
                                                $"{await RestRequests.RestRequester.LogHttpWebResponse(response, true)}", false);
                                        }
                                        bytesUploaded = position = newPosition;
                                        break;
                                    }
                                    else
                                    {
                                        this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                                        return;
                                    }
                            }
                        }
                    }
                }
                finally { F.CloseFileIfNot(); }
            }
            long ParseRangeHeader(MyHttpResponse response)
            {
                if (!response.Headers.ContainsKey("range")) return 0;
                else
                {
                    var s = response.Headers["range"];
                    const string keyword = "bytes=0-";
                    MyLogger.Assert(s.StartsWith(keyword));
                    return long.Parse(s.Substring(keyword.Length)) + 1;
                }
            }
            async Task StartResumableUploadAsync()
            {
                var request = new Api.Files.ResumableUpload(resumableUri, totalSize);
                using (var response = await request.GetHttpResponseAsync())
                {
                    switch (response?.StatusCode)
                    {
                        case System.Net.HttpStatusCode.OK:
                        case System.Net.HttpStatusCode.Created:
                            this.LogError("The upload was completed, and no further action is necessary.");
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return;
                        case System.Net.HttpStatusCode.NotFound:
                            this.LogError("The upload session has expired and the upload needs to be restarted from the beginning");
                            this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return;
                        default:
                            if ((int)response?.StatusCode == 308)
                            {
                                await StartResumableUploadAsync(ParseRangeHeader(response));
                                return;
                            }
                            else
                            {
                                this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                                return;
                            }
                    }
                }
            }
            public override async Task StartAsync(bool startFromScratch)
            {
                try
                {
                    if (IsCompleted)
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
                            return;
                        }
                        else
                        {
                            if (!await CreateResumableUploadAsync())
                            {
                                this.LogError("Failed to create resumable upload");
                                return;
                            }
                        }
                    }
                    if (resumableUri == null)
                    {
                        this.LogError("Seems upload not created yet, starting from scratch...");
                        await StartAsync(true);
                    }
                    else
                    {
                        await StartResumableUploadAsync();
                    }
                }
                finally { F.CloseFileIfNot(); }
            }
            public Uploader(File file)
            {
                F = file;
            }
        }
    }
}
