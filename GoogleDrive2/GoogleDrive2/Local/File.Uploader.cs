using System;
using System.Threading.Tasks;
using System.Collections;
using GoogleDrive2.Api;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public abstract partial class Uploader : Api.AdvancedApiOperator
        {
            const int MaxConcurrentCount = 7;
            public const long MinChunkSize = 262144;// + 1;
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<string> UploadCompleted;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
            protected void OnUploadCompleted(string id) { UploadCompleted?.Invoke(id); }
            public File F { get; protected set; }
            long? FileSize = null;
            private Func<Task<Api.Files.FullCloudFileMetadata>> GetFileMetadata = () => { return Task.FromResult(new Api.Files.FullCloudFileMetadata()); };
            public void SetFileMetadata(Func<Api.Files.FullCloudFileMetadata, Task<Api.Files.FullCloudFileMetadata>> func)
            {
                var preFunc = GetFileMetadata;
                GetFileMetadata = async () =>
                {
                    var metadata = await preFunc();
                    return metadata == null ? null : await func(metadata);
                };
            }
            protected async Task<long> GetFileSizeAsync()
            {
                if (!FileSize.HasValue) FileSize = (long)await F.GetSizeAsync();
                return FileSize.Value;
            }
            public async Task GetFileSizeFirstAsync()
            {
                ProgressChanged?.Invoke(new Tuple<long, long>(0, (long)await GetFileSizeAsync()));
            }
            protected abstract Task<bool> StartUploadAsync();
            static Libraries.MySemaphore semaphore = new Libraries.MySemaphore(MaxConcurrentCount);
            protected override async Task<bool> StartPrivateAsync()
            {
                await semaphore.WaitAsync();
                try
                {
                    F.CloseReadIfNot();
                    await AssignFileMetadata();
                    return await StartUploadAsync();
                }
                catch (Exception error)
                {
                    this.LogError(error.ToString());
                    return false;
                }
                finally
                {
                    F.CloseReadIfNot();
                    semaphore.Release();
                }
            }
            static volatile int InstanceCount = 0;
            public new static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
            static void AddInstanceCount(int value) { System.Threading.Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
            ~Uploader() { AddInstanceCount(-1); }
            protected Uploader(File file)
            {
                AddInstanceCount(1);
                F = file;
                this.UploadCompleted += (id) => { Debug($"{Constants.Icons.Completed} Upload Completed: fileId = \"{id}\""); };
                NewUploaderCreated?.Invoke(this);
            }
            public static async Task<Uploader> GetUploader(File file)
            {
                Uploader up = null;
                if (await file.GetSizeAsync() < Uploader.MinChunkSize) up = new MultipartUploader(file);
                else up = new ResumableUploader(file);
                return up;
            }
        }
    }
}
