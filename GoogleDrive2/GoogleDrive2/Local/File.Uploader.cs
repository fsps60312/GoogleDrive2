using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using GoogleDrive2.Api;
using System.Linq;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public class UploaderSchedulerPrototype
            {
                protected class UploaderQueue
                {
                    object syncRoot = new object();
                    SortedSet<Uploader> queue = new SortedSet<Uploader>();
                    public event Libraries.Events.MyEventHandler<int> CountChanged;
                    public void SetComparer(IComparer<Uploader> comparer)
                    {
                        lock (syncRoot)
                        {
                            var preQueue = queue;
                            queue = new SortedSet<Uploader>(preQueue, comparer);
                        }
                    }
                    public Uploader Dequeue()
                    {
                        lock (syncRoot)
                        {
                            var answer = queue.ElementAt(0);
                            MyLogger.Assert(queue.Remove(answer));
                            CountChanged?.Invoke(Count);
                            return answer;
                        }
                    }
                    public void Enqueue(Uploader uploader)
                    {
                        lock (syncRoot)
                        {
                            if(!queue.Add(uploader))
                            {
                                throw new NotImplementedException("!queue.Add(uploader)");
                            }
                            CountChanged?.Invoke(Count);
                        }
                    }
                    public int Count { get { lock (syncRoot) return queue.Count; } }
                    public bool Remove(Uploader uploader)
                    {
                        lock (syncRoot)
                        {
                            var answer = queue.Remove(uploader);
                            CountChanged?.Invoke(Count);
                            return answer;
                        }
                    }
                }
                protected UploaderQueue FileUploadQueue = new UploaderQueue();
                public event Libraries.Events.MyEventHandler<int> QueuedUploaderCountChanged, RunningFileUploadingCountChanged;
                public int MaxConcurrentCount { get; private set; }
                int __FileUploadingCount__ = 0;
                int FileUploadingCount
                {
                    get
                    {
                        return __FileUploadingCount__;
                    }
                    set
                    {
                        if (value == __FileUploadingCount__) return;
                        __FileUploadingCount__ = value;
                        RunningFileUploadingCountChanged?.Invoke(value);
                    }
                }
                object syncRoot = new object();
                protected UploaderSchedulerPrototype(int maxConcurrentCount)
                {
                    MaxConcurrentCount= maxConcurrentCount;
                    FileUploadQueue.CountChanged += (c) => QueuedUploaderCountChanged?.Invoke(c);
                }
                protected bool TrySeekAvailableUploadThread()
                {
                    lock(syncRoot)
                    {
                        if(FileUploadingCount<MaxConcurrentCount)
                        {
                            FileUploadingCount++;
                            return true;
                        }
                        else return false;
                    }
                }
                protected void FileUploadFinished()
                {
                    lock (syncRoot) FileUploadingCount--;
                }
            }
            public class UploaderScheduler: UploaderSchedulerPrototype
            {
                Libraries.Events.MyEventHandler<object> notifySchedulerReleaseEventHandler;
                public UploaderScheduler(int maxConcurrentCount):base(maxConcurrentCount)
                {
                    notifySchedulerReleaseEventHandler = new Libraries.Events.MyEventHandler<object>((sender) =>
                    {
                        lock (syncRoot)
                        {
                            FileUploadFinished();
                            FileUploadQueue.Remove(sender as Uploader);
                            UnregisterFileUploader(sender as Uploader);
                            CheckUploadQueue();
                        }
                    });
                }
                object syncRoot = new object();
                void CheckUploadQueue()
                {
                    lock (syncRoot)
                    {
                        if (FileUploadQueue.Count > 0)
                        {
                            if (TrySeekAvailableUploadThread())
                            {
                                FileUploadQueue.Dequeue().SchedulerReleaseSemaphore();
                            }
                        }
                    }
                }
                void RegisterFileUploader(Uploader uploader)
                {
                    uploader.NotifySchedulerRelease += notifySchedulerReleaseEventHandler;
                }
                void UnregisterFileUploader(Uploader uploader)
                {
                    uploader.NotifySchedulerRelease -= notifySchedulerReleaseEventHandler;
                }
                public void AddToQueueAndStart(Uploader uploader)
                {
                    lock (syncRoot)
                    {
                        RegisterFileUploader(uploader);
                        FileUploadQueue.Enqueue(uploader);
                        //MyLogger.Debug($"Queue Count = {FileUploadQueue.Count}");
                        CheckUploadQueue();
                    }
                }
            }
            static UploaderScheduler SmallFileUploaderScheduler = new UploaderScheduler(3), LargeFileUploaderScheduler = new UploaderScheduler(1);
            public static event Libraries.Events.MyEventHandler<int> QueuedSmallFileCountChanged, QueuedLargeFileCountChanged, RunningSmallFileUploadingCountChanged, RunningLargeFileUploadingCountChanged;
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
        public abstract partial class Uploader : Api.AdvancedApiOperator,IComparable
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
            private void SchedulerReleaseSemaphore() { semaphore.Release(); }
            private event Libraries.Events.MyEventHandler<object> NotifySchedulerRelease;
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
                finally { NotifySchedulerRelease?.Invoke(this); }
            }
            static int InstanceCount = 0;
            public new static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
            static void AddInstanceCount(int value) { System.Threading.Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
            ~Uploader() { AddInstanceCount(-1); }
            long SerialNumber;
            static long SerialNumberCounter = 0;
            protected Uploader(File file)
            {
                SerialNumber = Interlocked.Increment(ref SerialNumberCounter);
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

            public int CompareTo(object obj)
            {
                return SerialNumber.CompareTo((obj as Uploader).SerialNumber);
            }
        }
    }
}
