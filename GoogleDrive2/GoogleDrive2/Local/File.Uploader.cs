using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public abstract class UploaderPrototype : Api.AdvancedApiOperator
        {
            public event Libraries.Events.MyEventHandler<string> UploadCompleted;
            protected void OnUploadCompleted(string id)
            {
                UploadCompleted?.Invoke(id);
                OnCompleted(true);
            }
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
                    await StartUploadAsync(startFromScratch);
                }
                catch(Exception error) { this.LogError(error.ToString()); }
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
                        OnUploadCompleted(ParseCloudId(await request.GetResponseTextAsync(response)));
                    }
                    else this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                }
            }
            public MultipartUploader(File file, Api.Files.FullCloudFileMetadata fileMetadata) : base(file, fileMetadata) { }
        }
        public class ResumableUploader : UploaderPrototype
        {
            public const long FileBufferSize = 1 << 20;
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
            Queue<byte> fileContentBuffer = new Queue<byte>();
            long fileContentBufferPosition = 0;
            async Task<byte[]>ReadBytesAsync(long position,int chunkSize)
            {
                //await F.SeekReadAsync(position);
                //return await F.ReadBytesAsync(chunkSize);
                if (fileContentBufferPosition != position)
                {
                    MyLogger.Debug("Resumable: File Buffer Cleared");
                    fileContentBuffer.Clear();
                    fileContentBufferPosition = position;
                }
                await F.SeekReadAsync(fileContentBufferPosition + fileContentBuffer.Count);
                while (fileContentBuffer.Count < chunkSize)
                {
                    byte[] buffer = new byte[FileBufferSize];
                    int sz = await F.ReadAsync(buffer, 0, buffer.Length);
                    for (int i = 0; i < sz; i++) fileContentBuffer.Enqueue(buffer[i]);
                }
                byte[] answer = new byte[chunkSize];
                for (int i = 0; i < chunkSize; i++)
                {
                    answer[i] = fileContentBuffer.Dequeue();
                    fileContentBufferPosition++;
                }
                return answer;
            }
            async Task<MyHttpResponse> DoSingleResumableUploadAsync(long position, long chunkSize)
            {
                var request = new Api.Files.ResumableUpload(resumableUri, TotalSize, position, position + chunkSize - 1, await ReadBytesAsync(position,(int)chunkSize));
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
                                    BytesUploaded = TotalSize;
                                    OnUploadCompleted(ParseCloudId(await response.GetResponseString()));
                                    break;
                                default:
                                    if ((int?)response?.StatusCode == 308)
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
                                        OnCompleted(false);
                                        return;
                                    }
                            }
                        }
                        if (CheckPause()) return;
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
                            this.Debug("The upload was completed, and no further action is necessary.");
                            this.Debug(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return;
                        case System.Net.HttpStatusCode.NotFound:
                            this.Debug("The upload session has expired and the upload needs to be restarted from the beginning");
                            this.Debug(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return;
                        default:
                            if ((int?)response?.StatusCode == 308)
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
                if (CheckPause()) return;
                if (startFromScratch)
                {
                    if (!await CreateResumableUploadAsync())
                    {
                        this.LogError("Failed to create resumable upload");
                        return;
                    }
                }
                if (CheckPause()) return;
                if (resumableUri == null)
                {
                    this.Debug("Seems upload not created yet, starting from scratch...");
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
            public event Libraries.Events.MyEventHandler<string> UploadCompleted;
            public File F { get; private set; }
            public Func<Task<Api.Files.FullCloudFileMetadata>> GetFileMetadata
            {
                get;private set;
            } = () => { return Task.FromResult(new Api.Files.FullCloudFileMetadata()); };
            public void SetFileMetadata(Func<Api.Files.FullCloudFileMetadata,Task<Api.Files.FullCloudFileMetadata>>func)
            {
                var preFunc = GetFileMetadata;
                GetFileMetadata = async () =>
                  {
                      return await func(await preFunc());
                  };
            }
            UploaderPrototype up = null;
            protected override Task StartPrivateAsync(bool startFromScratch)
            {
                //should not be called
                throw new NotImplementedException("Should not be called");
            }
            public new async Task StartAsync(bool startFromScratch)
            {
                if (up == null)
                {
                    if (await F.GetSizeAsync() < UploaderPrototype.MinChunkSize) up = new MultipartUploader(F, await this.GetFileMetadata());
                    else up = new ResumableUploader(F, await this.GetFileMetadata());
                    up.Started += () => OnStarted();
                    up.ProgressChanged += (p) => ProgressChanged?.Invoke(p);
                    up.Paused += () =>
                    {
                        OnDebugged("Paused");
                        OnPaused();
                    };
                    up.Pausing += () =>
                    {
                        OnDebugged("Pausing...");
                        OnPausing();
                    };
                    up.Completed += (success) => OnCompleted(success);
                    up.Debugged += (msg) => OnDebugged(msg);
                    up.ErrorLogged += (msg) => OnErrorLogged(msg);// No need to add stacktrace again
                    up.UploadCompleted += (id) => UploadCompleted?.Invoke(id);
                    //up.MessageAppended is triggered by Debug & LogError
                }
                await up.StartAsync(startFromScratch);
            }
            public new void Pause() { up.Pause(); }
            public new bool IsActive { get { return up.IsActive; } }
            public Uploader(File file)
            {
                F = file;
                NewUploaderCreated?.Invoke(this);
            }
        }
    }
}
