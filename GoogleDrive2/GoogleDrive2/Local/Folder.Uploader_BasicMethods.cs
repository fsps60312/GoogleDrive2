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
            List<Subtask> Subtasks = new List<Subtask>();
            Tuple<long, long> MaintainProgress(Tuple<long, long> p, ref long current, ref long total, ref long parentCurrent, ref long parentTotal)
            {
                var cdif = p.Item1 - Interlocked.Exchange(ref current, p.Item1);
                var tdif = p.Item2 - Interlocked.Exchange(ref total, p.Item2);
                return new Tuple<long, long>(
                    Interlocked.Add(ref parentCurrent, cdif),
                    Interlocked.Add(ref parentTotal, tdif));
            }
            enum ProgressType { File, Folder, Size,LocalSearch };
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
                        RunningTaskCountChanged?.Invoke(new Tuple<long, long>(
                            Interlocked.Increment(ref ThreadCount), NotCompleted));
                    }
                };
                subtask.Pausing += () => { Interlocked.CompareExchange(ref isPausing, 1, 0); };
                subtask.Paused += () =>
                {
                    if (Interlocked.CompareExchange(ref isPausing, 0, 1) != 1) this.LogError("isPausing==0, but Paused triggered");
                    var threadCount = Interlocked.Decrement(ref ThreadCount);
                    RunningTaskCountChanged?.Invoke(new Tuple<long, long>(threadCount, NotCompleted));
                    if (threadCount == 0) this.OnPaused();
                };
                subtask.Completed += () =>
                {
                    Interlocked.CompareExchange(ref isPausing, 0, 1);
                    var threadCount = Interlocked.Decrement(ref ThreadCount);
                    var notCompleted = Interlocked.Decrement(ref NotCompleted);
                    RunningTaskCountChanged?.Invoke(new Tuple<long, long>(threadCount, notCompleted));
                    if (notCompleted == 0) this.OnCompleted(true);
                };
                RegisterProgressChange(subtask, ProgressType.File);
                RegisterProgressChange(subtask, ProgressType.Folder);
                RegisterProgressChange(subtask, ProgressType.Size);
                RegisterProgressChange(subtask, ProgressType.LocalSearch);
                this.Started += async () => { await subtask.Start(); };
                this.Pausing += () => { subtask.Pause(); };
                Subtasks.Add(subtask);
            }
            Subtask AddAndGetSubtask(File.Uploader uploader)
            {
                Action pausedCall;
                Action<bool> completedCall;
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall,localSearchStatusCall;
                var subtask = new Subtask(
                    new Func<Task>(async () => { await uploader.StartAsync(); }),
                    new Action(() => { uploader.Pause(); }),
                    out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall,out localSearchStatusCall);
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
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall, localSearchStatusCall;
                var subtask = new Subtask(
                    new Func<Task>(async () => { await uploader.StartAsync(); }),
                    new Action(() => { uploader.Pause(); }),
                    out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall,out localSearchStatusCall);
                uploader.Paused += () => { pausedCall(); };
                uploader.Completed += (success) => { completedCall(success); };
                uploader.FileProgressChanged += (p) => { fileProgressCall(p); };
                uploader.FolderProgressChanged += (p) => { folderProgressCall(p); };
                uploader.SizeProgressChanged += (p) => { sizeProgressCall(p); };
                uploader.LocalSearchStatusChanged += (p) => { localSearchStatusCall(p); };
                AddSubtask(subtask);
                return subtask;
            }
            int CreateFolderTaskProgress = 0;
            int IsCreateFolderTaskInProgress = 0;
            private async Task CreateFolderTask()
            {
                if (Interlocked.CompareExchange(ref IsCreateFolderTaskInProgress, 1, 0) == 1) return;
                try
                {
                    if (0 == CreateFolderTaskProgress)
                    {
                        await folderCreator.StartAsync();
                        return;//folderCreator.Completed will maintain CreateFolderTaskProgress
                    }
                }
                finally { MyLogger.Assert(Interlocked.CompareExchange(ref IsCreateFolderTaskInProgress, 0, 1) == 1); }
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
                        this.Debug("Searching subfolders...");
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                        var subfolders = await F.GetFoldersAsync();
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Decrement(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                        this.Debug($"Found {subfolders.Count} subfolders");
                        Interlocked.Add(ref NotCompleted, subfolders.Count);
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
                        Interlocked.Add(ref NotCompleted, subfiles.Count);
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
