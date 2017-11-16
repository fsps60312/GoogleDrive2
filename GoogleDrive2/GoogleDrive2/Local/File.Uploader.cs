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
        public partial class Uploader : Libraries.MyQueuedTask
        {
            static Libraries.MyTaskQueue SmallFileUploaderScheduler = new MyTaskQueue(3), LargeFileUploaderScheduler = new MyTaskQueue(1);
            public static event Libraries.Events.MyEventHandler<int> QueuedSmallFileCountChanged, QueuedLargeFileCountChanged, RunningSmallFileUploadingCountChanged, RunningLargeFileUploadingCountChanged;
            static Uploader()
            {
                SmallFileUploaderScheduler.QueuedUploaderCountChanged += (c) => QueuedSmallFileCountChanged?.Invoke(c);
                LargeFileUploaderScheduler.QueuedUploaderCountChanged += (c) => QueuedLargeFileCountChanged?.Invoke(c);
                SmallFileUploaderScheduler.RunningFileUploadingCountChanged += (c) => RunningSmallFileUploadingCountChanged?.Invoke(c);
                LargeFileUploaderScheduler.RunningFileUploadingCountChanged += (c) => RunningLargeFileUploadingCountChanged?.Invoke(c);
            }
            public event MyEventHandler<object> NotifySchedulerCompleted;
            public event MyEventHandler<object> RemoveFromTaskQueueRequested;
            void MyQueuedTask.SchedulerReleaseSemaphore()
            {
                semaphore.Release();
            }
            long SerialNumber;
            static long SerialNumberCounter = 0;
            public int CompareTo(object obj)
            {
                return SerialNumber.CompareTo((obj as Uploader).SerialNumber);
            }
            void MyQueuedTaskInitialize()
            {
                SerialNumber = Interlocked.Increment(ref SerialNumberCounter);
                this.Pausing += delegate { RemoveFromTaskQueueRequested?.Invoke(this); };
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
        public abstract partial class Uploader : Api.AdvancedApiOperator
        {
            public const long MinChunkSize = 262144;// + 1;
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<string> UploadCompleted;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
            protected void OnUploadCompleted(string id) { UploadCompleted?.Invoke(id); }
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
            protected abstract Task<bool> StartUploadAsync(Api.Files.FullCloudFileMetadata metadata);
            Libraries.MySemaphore semaphore = new Libraries.MySemaphore(0);
            protected override async Task<bool> StartPrivateAsync()
            {
                AddWaitingForMetadataCount(1);
                var metadata = await GetFileMetadata();
                AddWaitingForMetadataCount(-1);
                if (CheckPause() || metadata == null) return false;
                AddQueuedCount(1);
                if (this is MultipartUploader) SmallFileUploaderScheduler.AddToQueueAndStart(this);
                else LargeFileUploaderScheduler.AddToQueueAndStart(this);
                try
                {
                    await semaphore.WaitAsync();
                    AddQueuedCount(-1);
                    if (CheckPause()) return false;
                    AddRunningCount(1);
                    try
                    {
                        F.CloseReadIfNot();
                        TotalSize = await this.GetFileSizeAsync();
                        return await StartUploadAsync(metadata);
                    }
                    catch (Exception error)
                    {
                        this.LogError(error.ToString());
                        return false;
                    }
                    finally
                    {
                        F.CloseReadIfNot();
                        AddRunningCount(-1);
                    }
                }
                finally { NotifySchedulerCompleted?.Invoke(this); }
            }
            static int InstanceCount = 0;
            public new static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
            static void AddInstanceCount(int value) { System.Threading.Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
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
                this.UploadCompleted += (id) => { Debug($"{Constants.Icons.Completed} Upload Completed: fileId = \"{id}\""); };
                MyQueuedTaskInitialize();
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
