using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace GoogleDrive2.Local
{
    partial class Folder
    {
        public partial class Uploader : Api.AdvancedApiOperator
        {
            long ProgressCurrentFile = 0, ProgressTotalFile = 0;
            long ProgressCurrentFolder = 0, ProgressTotalFolder = 1;
            long ProgressCurrentSize = 0, ProgressTotalSize = 0;
            long SearchLocalFoldersActions = 0, SearchLocalFilesActions = 0;
            long ThreadCount = 0, NotCompleted = 0, AddedThreadCount = 0;
            void OnRunningTaskCountChanged(Tuple<long,long>rawData)
            {
                RunningTaskCountChanged?.Invoke(new Tuple<long, long>(rawData.Item1 - Interlocked.Read(ref AddedThreadCount), rawData.Item2));
            }
            void AddThreadCount(long v)
            {
                var threadCount = Interlocked.Add(ref ThreadCount, v);
                OnRunningTaskCountChanged(new Tuple<long, long>(threadCount, NotCompleted));
                if (threadCount == 0)
                {
                    MyLogger.Assert(NotCompleted >= 0);
                    if (NotCompleted == 0) OnCompleted(true);
                    else CheckPause();
                }
            }
            void AddNotCompleted(long v)
            {
                var notCompleted = Interlocked.Add(ref NotCompleted, v);
                OnRunningTaskCountChanged(new Tuple<long, long>(ThreadCount, notCompleted));
            }
            List<Subtask> Subtasks = new List<Subtask>();
            Tuple<long, long> MaintainProgress(Tuple<long, long> p, ref long current, ref long total, ref long parentCurrent, ref long parentTotal)
            {
                var cdif = p.Item1 - Interlocked.Exchange(ref current, p.Item1);
                var tdif = p.Item2 - Interlocked.Exchange(ref total, p.Item2);
                return new Tuple<long, long>(
                    Interlocked.Add(ref parentCurrent, cdif),
                    Interlocked.Add(ref parentTotal, tdif));
            }
            enum ProgressType { File, Folder, Size,LocalSearch ,RunningTaskCount};
            void RegisterProgressChange(Subtask subtask, ProgressType progressType)
            {
                long current = 0, total = 0;
                switch (progressType)
                {
                    case ProgressType.File:
                        subtask.FileProgressChanged += (p) =>
                        {
                            this.FileProgressChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref ProgressCurrentFile, ref ProgressTotalFile));
                        };
                        break;
                    case ProgressType.Folder:
                        subtask.FolderProgressChanged += (p) =>
                        {
                            this.FolderProgressChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref ProgressCurrentFolder, ref ProgressTotalFolder));
                        };
                        break;
                    case ProgressType.Size:
                        subtask.SizeProgressChanged += (p) =>
                        {
                            this.SizeProgressChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref ProgressCurrentSize, ref ProgressTotalSize));
                        };
                        break;
                    case ProgressType.LocalSearch:
                        subtask.LocalSearchStatusChanged += (p) =>
                        {
                            this.LocalSearchStatusChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref SearchLocalFoldersActions, ref SearchLocalFilesActions));
                        };
                        break;
                    case ProgressType.RunningTaskCount:
                        subtask.RunningTaskCountChanged += (p) =>
                        {
                            this.OnRunningTaskCountChanged(MaintainProgress(p,
                                ref current, ref total, ref ThreadCount, ref NotCompleted));
                        };
                        break;
                    default: MyLogger.LogError($"Unexpected progressType: {progressType}"); break;
                }
            }
            void AddSubtask(Subtask subtask)
            {
                int isPausing = 0;
                subtask.Started += () =>
                {
                    if (Interlocked.CompareExchange(ref isPausing, 0, 1) == 0)
                    {
                        AddThreadCount(1);
                    }
                };
                subtask.Pausing += () => { Interlocked.CompareExchange(ref isPausing, 1, 0); };
                subtask.Paused += () =>
                {
                    if (Interlocked.CompareExchange(ref isPausing, 0, 1) != 1) this.LogError("isPausing==0, but Paused triggered");
                    AddThreadCount(-1);
                };
                subtask.Completed += () =>
                {
                    Interlocked.CompareExchange(ref isPausing, 0, 1);
                    AddThreadCount(-1);
                    AddNotCompleted(-1);
                };
                RegisterProgressChange(subtask, ProgressType.File);
                RegisterProgressChange(subtask, ProgressType.Folder);
                RegisterProgressChange(subtask, ProgressType.Size);
                RegisterProgressChange(subtask, ProgressType.LocalSearch);
                RegisterProgressChange(subtask, ProgressType.RunningTaskCount);
                this.Started += async () => { await subtask.Start(); };
                this.Pausing += () => { subtask.Pause(); };
                Subtasks.Add(subtask);
            }
            Subtask AddAndGetSubtask(File.Uploader uploader)
            {
                Action pausedCall;
                Action<bool> completedCall;
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall,localSearchStatusCall,runningTaskCountCall;
                var subtask = new Subtask(
                    new Func<Task>(async () => { await uploader.StartAsync(); }),
                    new Action(() => { uploader.Pause(); }),
                    out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall, out localSearchStatusCall, out runningTaskCountCall);
                uploader.Paused += () => { pausedCall(); };
                uploader.Completed += (success) =>
                {
                    completedCall(success);
                    fileProgressCall(new Tuple<long, long>(1, 1));
                };
                uploader.ProgressChanged += (p) => { sizeProgressCall(p); };
                AddSubtask(subtask);
                fileProgressCall(new Tuple<long, long>(0, 1));
                return subtask;
            }
            Subtask AddAndGetSubtask(Folder.Uploader uploader)
            {
                Action pausedCall;
                Action<bool> completedCall;
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall, localSearchStatusCall,runningTaskCountCall;
                var subtask = new Subtask(
                    new Func<Task>(async () => { await uploader.StartAsync(); }),
                    new Action(() => { uploader.Pause(); }),
                    out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall, out localSearchStatusCall, out runningTaskCountCall);
                uploader.Paused += () => { pausedCall(); };
                uploader.Completed += (success) => { completedCall(success); };
                uploader.FileProgressChanged += (p) => { fileProgressCall(p); };
                uploader.FolderProgressChanged += (p) => { folderProgressCall(p); };
                uploader.SizeProgressChanged += (p) => { sizeProgressCall(p); };
                uploader.LocalSearchStatusChanged += (p) => { localSearchStatusCall(p); };
                uploader.RunningTaskCountChanged += (p) => { runningTaskCountCall(p); };
                AddSubtask(subtask);
                return subtask;
            }
            int CreateFolderTaskProgress = 0;
            int IsCreateFolderTaskInProgress = 0;
            Libraries.MySemaphore semaphoreCreateFolder = new Libraries.MySemaphore(1);
            private async Task CreateFolderTask()
            {
                if (Interlocked.CompareExchange(ref IsCreateFolderTaskInProgress, 1, 0) == 1) return;
                await semaphoreCreateFolder.WaitAsync();
                try
                {
                    if (0 == CreateFolderTaskProgress)
                    {
                        if (IsPausing) return;
                        AddThreadCount(1);
                        await folderCreator.StartAsync();//folderCreator.Completed will do: AddThreadCount(-1), AddNotCompleted(-1) if necessary
                        return;//folderCreator.Completed will maintain CreateFolderTaskProgress
                    }
                }
                finally
                {
                    semaphoreCreateFolder.Release();
                    MyLogger.Assert(Interlocked.CompareExchange(ref IsCreateFolderTaskInProgress, 0, 1) == 1);
                }
            }
            int UploadSubfoldersTaskProgress = 0;
            int IsUploadSubfoldersTaskInProgress = 0;
            private async Task UploadSubfoldersTask()
            {
                if (Interlocked.CompareExchange(ref IsUploadSubfoldersTaskInProgress, 1, 0) == 1) return;
                try
                {
                    if (0 == UploadSubfoldersTaskProgress)
                    {
                        if (IsPausing) return;
                        this.Debug("Searching subfolders...");
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                        var subfolders = await F.GetFoldersAsync();
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Decrement(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                        this.Debug($"Found {subfolders.Count} subfolders");
                        AddNotCompleted(subfolders.Count);
                        await Task.WhenAll(subfolders.Select(async (f) =>
                        {
                            var uploader = new Folder.Uploader(f);
                            uploader.folderCreator.SetFolderMetadata(async (metadata) =>
                            {
                                metadata.parents = new List<string> { await folderCreator.GetCloudId() };
                                return metadata;
                            });
                            var subtask = AddAndGetSubtask(uploader);
                            if (this.IsActive) await subtask.Start();
                        }));
                        UploadSubfoldersTaskProgress++;
                    }
                }
                finally { MyLogger.Assert(Interlocked.CompareExchange(ref IsUploadSubfoldersTaskInProgress, 0, 1) == 1); }
            }
            int UploadSubfilesTaskProgress = 0;
            int IsUploadSubfilesTaskInProgress = 0;
            private async Task UploadSubfilesTask()
            {
                if (Interlocked.CompareExchange(ref IsUploadSubfilesTaskInProgress, 1, 0) == 1) return;
                try
                {
                    if (0 == UploadSubfilesTaskProgress)
                    {
                        this.Debug("Searching subfiles...");
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Increment(ref this.SearchLocalFilesActions)));
                        var subfiles = await F.GetFilesAsync();
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Add(ref this.SearchLocalFilesActions, -1 + subfiles.Count)));
                        this.Debug($"Found {subfiles.Count} subfiles");
                        AddNotCompleted(subfiles.Count);
                        await Task.WhenAll(subfiles.Select(async (f) =>
                        {
                            var uploader = new File.Uploader(f);
                            uploader.SetFileMetadata(async (metadata) =>
                            {
                                metadata.parents = new List<string> { await folderCreator.GetCloudId() };
                                return metadata;
                            });
                            var subtask = AddAndGetSubtask(uploader);
                            await uploader.GetFileSizeFirstAsync();
                            LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Decrement(ref this.SearchLocalFilesActions)));
                            if (this.IsActive) await subtask.Start();
                        }));
                        UploadSubfilesTaskProgress++;
                    }
                }
                finally { MyLogger.Assert(Interlocked.CompareExchange(ref IsUploadSubfilesTaskInProgress, 0, 1) == 1); }
            }
        }
    }
}
