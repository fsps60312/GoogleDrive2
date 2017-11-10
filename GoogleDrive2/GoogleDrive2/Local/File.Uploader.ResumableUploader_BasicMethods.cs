using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public partial class ResumableUploader : Uploader
            {
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
                public async Task<bool> CreateResumableUploadAsync()
                {
                    //await MyLogger.Alert($"file size: {totalSize}");
                    var metadata = await GetFileMetadata();
                    if (CheckPause() || metadata == null) return false;
                    var request = new Api.Files.ResumableCreate(metadata, TotalSize, null);
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
                async Task<byte[]> ReadBytesAsync(long position, int chunkSize)
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
                    var request = new Api.Files.ResumableUpload(resumableUri, TotalSize, position, position + chunkSize - 1, await ReadBytesAsync(position, (int)chunkSize));
                    var ans = await request.GetHttpResponseAsync();
                    request.ClearBody();
                    return ans;
                }
                async Task<bool> StartResumableUploadAsync(long position)
                {
                    try
                    {
                        long chunkCount = 1;
                        while (true)
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
                                        MyLogger.Assert(position == TotalSize);
                                        OnUploadCompleted(ParseCloudId(await response.GetResponseString()));
                                        return true;
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
                                            return false;
                                        }
                                }
                            }
                            if (CheckPause()) return false;
                        }
                    }
                    catch(Exception error)
                    {
                        this.LogError($"Error in async Task<bool> StartResumableUploadAsync(long position):\r\n{error}");
                        return false;
                    }
                    finally { F.CloseReadIfNot(); }
                }
                private async Task<bool> StartResumableUploadAsync()
                {
                    var request = new Api.Files.ResumableUpload(resumableUri, TotalSize);
                    long startPosition = -1;
                    using (var response = await request.GetHttpResponseAsync())
                    {
                        switch (response?.StatusCode)
                        {
                            case System.Net.HttpStatusCode.OK:
                            case System.Net.HttpStatusCode.Created:
                                this.Debug(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                                this.Debug($"{Constants.Icons.Info} The upload was completed, and no further action is necessary.");
                                return false;
                            case System.Net.HttpStatusCode.NotFound:
                                this.Debug(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                                this.Debug($"{Constants.Icons.Warning} The upload session has expired and the upload needs to be restarted from the beginning");
                                startPosition = 0;
                                break;
                            default:
                                if ((int?)response?.StatusCode == 308)
                                {
                                    startPosition = ParseRangeHeader(response);
                                    break;
                                }
                                else
                                {
                                    this.LogError(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                                    return false;
                                }
                        }
                    }
                    MyLogger.Assert(startPosition != -1);
                    return await StartResumableUploadAsync(startPosition);
                }
            }
        }
    }
}
