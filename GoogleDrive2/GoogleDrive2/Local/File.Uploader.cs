using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public abstract class UploaderPrototype : Api.AdvancedApiOperator
        {
            public static async Task StartPrivateStaticAsync(UploaderPrototype up, bool startFromScratch)
            {
                await up.StartPrivateAsync(startFromScratch);
            }
            public const long MinChunkSize = 262144;// + 1;
            protected File F;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
            protected Api.Files.FullCloudFileMetadata FileMetadata;
            private long __BytesUploaded__ = 0;
            protected long BytesUploaded
            {
                get { return __BytesUploaded__; }
                set
                {
                    if (__BytesUploaded__ == value) return;
                    __BytesUploaded__ = value;
                    ProgressChanged?.Invoke(Tuple.Create(value, TotalSize));
                }
            }
            private long __TotalSize__ = 0;
            protected long TotalSize
            {
                get { return __TotalSize__; }
                set
                {
                    if (__TotalSize__ == value) return;
                    __TotalSize__ = value;
                    ProgressChanged?.Invoke(Tuple.Create(BytesUploaded, value));
                }
            }
            protected async Task AssignFileMetadata()
            {
                TotalSize = (long)await F.GetSizeAsync();
                FileMetadata.name = F.Name;
                FileMetadata.createdTime = await F.GetTimeCreatedAsync();
                FileMetadata.modifiedTime = await F.GetTimeModifiedAsync();
            }
            protected string ParseCloudId(string content)
            {
                return JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(content).id;
            }
            protected abstract Task StartUploadAsync(bool startFromScratch);
            protected override async Task StartPrivateAsync(bool startFromScratch)
            {
                try
                {
                    F.CloseReadIfNot();
                    await AssignFileMetadata();
                    BytesUploaded = 0;
                    await StartUploadAsync(startFromScratch);
                }
                finally { F.CloseReadIfNot(); }
            }
            public UploaderPrototype(File file, Api.Files.FullCloudFileMetadata fileMetadata)
            {
                F = file;
                FileMetadata = fileMetadata;
            }
        }
        public class MultipartUploader : UploaderPrototype
        {
            protected override async Task StartUploadAsync(bool startFromScratch)
            {
                MyLogger.Assert(BytesUploaded == 0 && TotalSize <= int.MaxValue);
                var request = new Api.Files.MultipartUpload(FileMetadata, await F.ReadBytesAsync((int)TotalSize));
                F.CloseReadIfNot();
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        BytesUploaded = TotalSize;
                        OnCompleted(ParseCloudId(await request.GetResponseTextAsync(response)));
                    }
                    else this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                }
            }
            public MultipartUploader(File file, Api.Files.FullCloudFileMetadata fileMetadata) : base(file, fileMetadata) { }
        }
        public class ResumableUploader : UploaderPrototype
        {
            public const double DesiredProgressUpdateInterval = 0.5;
            string resumableUri = null;
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
            async Task<bool> CreateResumableUploadAsync()
            {
                //await MyLogger.Alert($"file size: {totalSize}");
                var request = new Api.Files.ResumableCreate(FileMetadata, TotalSize, null);
                using (var response = await request.GetHttpResponseAsync())
                {
                    if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        resumableUri = response.Headers["location"];
                        return true;
                    }
                    else
                    {
                        this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                        return false;
                    }
                }
            }
            async Task<MyHttpResponse> DoSingleResumableUploadAsync(long position, long chunkSize)
            {
                await F.SeekReadAsync(position);
                var request = new Api.Files.ResumableUpload(resumableUri, TotalSize, position, position + chunkSize - 1, await F.ReadBytesAsync((int)chunkSize));
                var ans = await request.GetHttpResponseAsync();
                request.ClearBody();
                return ans;
            }
            async Task StartResumableUploadAsync(long position)
            {
                try
                {
                    long chunkCount = 1;
                    while (position < TotalSize)
                    {
                        var time = DateTime.Now;
                        var realChunkSize = Math.Min(TotalSize - position, chunkCount * MinChunkSize);
                        using (var response = await DoSingleResumableUploadAsync(position, realChunkSize))
                        {
                            var prePosition = position;
                            position += realChunkSize;
                            if ((DateTime.Now - time).TotalSeconds < DesiredProgressUpdateInterval) chunkCount += (chunkCount + 1) / 2;
                            else chunkCount = (chunkCount + 1) / 2;
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
                                            this.LogError($"Server is expected to read {position - prePosition}({position}) bytes," +
                                                $" {newPosition - prePosition}({newPosition}) bytes actually\r\n" +
                                                $"{await RestRequests.RestRequester.LogHttpWebResponse(response, true)}", false);
                                        }
                                        BytesUploaded = position = newPosition;
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
                finally { F.CloseReadIfNot(); }
            }
            async Task StartResumableUploadAsync()
            {
                var request = new Api.Files.ResumableUpload(resumableUri, TotalSize);
                long startPosition = -1;
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
                                startPosition = ParseRangeHeader(response);
                                break;
                            }
                            else
                            {
                                this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                                return;
                            }
                    }
                }
                MyLogger.Assert(startPosition != -1);
                await StartResumableUploadAsync(startPosition);
            }
            protected override async Task StartUploadAsync(bool startFromScratch)
            {
                if (startFromScratch)
                {
                    if (!await CreateResumableUploadAsync())
                    {
                        this.LogError("Failed to create resumable upload");
                        return;
                    }
                }
                if (resumableUri == null)
                {
                    this.LogError("Seems upload not created yet, starting from scratch...");
                    await StartUploadAsync(true);
                }
                else
                {
                    await StartResumableUploadAsync();
                }
            }
            public ResumableUploader(File file, Api.Files.FullCloudFileMetadata fileMetadata) : base(file, fileMetadata) { }
        }
        public class Uploader:Api.AdvancedApiOperator
        {
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
            public File F { get; private set; }
            public Api.Files.FullCloudFileMetadata FileMetadata
            {
                get;private set;
            } = new Api.Files.FullCloudFileMetadata();
            UploaderPrototype up = null;
            protected override async Task StartPrivateAsync(bool startFromScratch)
            {
                if(up==null)
                {
                    if (await F.GetSizeAsync() < UploaderPrototype.MinChunkSize) up = new MultipartUploader(F, this.FileMetadata);
                    else up = new ResumableUploader(F, this.FileMetadata);
                    up.ProgressChanged += (p) => ProgressChanged?.Invoke(p);
                    up.Paused += () => OnPaused();
                    up.Pausing += () => OnPausing();
                    // TODO add other events
                }
                await this.RunLogger(up, UploaderPrototype.StartPrivateStaticAsync(up, startFromScratch));
            }
            public Uploader(File file)
            {
                F = file;
            }
        }
    }
}
