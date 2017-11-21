using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using GoogleDrive2.Api;
using System.Linq;
using GoogleDrive2.Libraries;
using GoogleDrive2.Libraries.Events;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader : MyTask
        {
            static Libraries.MyTaskQueue SmallFileUploaderScheduler = new MyTaskQueue(7), LargeFileUploaderScheduler = new MyTaskQueue(2);
            public static event Libraries.Events.MyEventHandler<long> QueuedSmallFileCountChanged, QueuedLargeFileCountChanged, RunningSmallFileUploadingCountChanged, RunningLargeFileUploadingCountChanged;
            static Uploader()
            {
                SmallFileUploaderScheduler.QueuedUploaderCountChanged += (c) => QueuedSmallFileCountChanged?.Invoke(c);
                LargeFileUploaderScheduler.QueuedUploaderCountChanged += (c) => QueuedLargeFileCountChanged?.Invoke(c);
                SmallFileUploaderScheduler.RunningFileUploadingCountChanged += (c) => RunningSmallFileUploadingCountChanged?.Invoke(c);
                LargeFileUploaderScheduler.RunningFileUploadingCountChanged += (c) => RunningLargeFileUploadingCountChanged?.Invoke(c);
            }
        }
        public partial class Uploader
        {
            static int RunningCount = 0, QueuedCount = 0, WaitingForMetadataCount = 0;
            public static event Libraries.Events.MyEventHandler<int> RunningCountChanged,QueuedCountChanged,WaitingForMetadataCountChanged;
            static void AddRunningCount(int value) { System.Threading.Interlocked.Add(ref RunningCount, value); RunningCountChanged?.Invoke(RunningCount); }
            static void AddQueuedCount(int value) { System.Threading.Interlocked.Add(ref QueuedCount, value); QueuedCountChanged?.Invoke(QueuedCount); }
            static void AddWaitingForMetadataCount(int value) { System.Threading.Interlocked.Add(ref WaitingForMetadataCount, value); WaitingForMetadataCountChanged?.Invoke(WaitingForMetadataCount); }
        }
        public abstract partial class Uploader
        {
            public const long MinChunkSize = 262144;// + 1;
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<string> UploadCompleted;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
            protected void OnUploadCompleted(string id) { OnCompleted(); UploadCompleted?.Invoke(id); }
            public File F { get; protected set; }
            public long? FileSize { get; private set; } = null;
            private Func<Task<Api.Files.FullCloudFileMetadata>> GetFileMetadata;
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
            public async Task<long> GetFileSizeFirstAsync()
            {
                var sz = (long)await GetFileSizeAsync();
                ProgressChanged?.Invoke(new Tuple<long, long>(0, sz));
                return sz;
            }
            protected abstract Task StartUploadAsync(Api.Files.FullCloudFileMetadata metadata);
            Api.Files.FullCloudFileMetadata metadataForMainTask = null;
            protected override async Task PrepareBeforeStartAsync()
            {
                AddWaitingForMetadataCount(1);
                try
                {
                    metadataForMainTask = await GetFileMetadata();
                }
                finally { AddWaitingForMetadataCount(-1); }
            }
            protected override async Task StartMainTaskAsync()
            {
                if (IsPausing || metadataForMainTask == null) return;
                AddRunningCount(1);
                try
                {
                    F.CloseReadIfNot();
                    TotalSize = await this.GetFileSizeAsync();
                    await StartUploadAsync(metadataForMainTask);
                }
                catch (Exception error)
                {
                    this.LogError(error.ToString());
                }
                finally
                {
                    F.CloseReadIfNot();
                    AddRunningCount(-1);
                }
            }
            static int InstanceCount = 0;
            public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
            static void AddInstanceCount(int value) { Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
            ~Uploader() { AddInstanceCount(-1); }
            protected Uploader(File file)
            {
                AddInstanceCount(1);
                F = file;
                GetFileMetadata = async () =>
                {
                    var metadata = new Api.Files.FullCloudFileMetadata();
                    metadata.name = F.Name;
                    metadata.createdTime = await F.GetTimeCreatedAsync();
                    metadata.modifiedTime = await F.GetTimeModifiedAsync();
                    return metadata;
                };
                this.Started += delegate { this.Debug($"{Constants.Icons.Info} Started"); };
                this.Pausing += delegate { this.Debug($"{Constants.Icons.Pausing} Pausing..."); };
                this.UploadCompleted += (id) => { Debug($"{Constants.Icons.Completed} Upload Completed: fileId = \"{id}\""); };
                this.Queued += delegate { this.Debug($"{Constants.Icons.Info} Queued"); AddQueuedCount(1); };
                this.Unqueued += delegate { this.Debug($"{Constants.Icons.Info} Unqueued"); AddQueuedCount(-1); };
                this.TaskQueue = this is MultipartUploader ? SmallFileUploaderScheduler : LargeFileUploaderScheduler;
                NewUploaderCreated?.Invoke(this);
            }
            public static async Task<Uploader> GetUploader(File file)
            {
                Uploader up = null;
                var fileSize = await file.GetSizeAsync();
                if (fileSize <= Uploader.MinChunkSize) up = new MultipartUploader(file);
                else up = new ResumableUploader(file);
                up.FileSize = (long)fileSize;
                return up;
            }
        }
    }
}
