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
            long ProgressCurrentFolder = 0, ProgressTotalFolder = 0;
            long ProgressCurrentSize = 0, ProgressTotalSize = 0;
            long SearchLocalFoldersActions = 0, SearchLocalFilesActions = 0;
            long ThreadCount = 0, NotCompleted = 0, AddedThreadCount = 0;
            void OnRunningTaskCountChanged(Tuple<long,long>rawData)
            {
                RunningTaskCountChanged?.Invoke(new Tuple<long, long>(rawData.Item1 - Interlocked.Read(ref AddedThreadCount), rawData.Item2));
            }
            bool? AddThreadCount(long v)
            {
                var threadCount = Interlocked.Add(ref ThreadCount, v);
                OnRunningTaskCountChanged(new Tuple<long, long>(threadCount, NotCompleted));
                if (threadCount == 0)//If threadCount==0, NotCompleted will not changed
                {
                    MyLogger.Assert(NotCompleted >= 0);
                    return NotCompleted == 0;
                }
                return null;
            }
            void AddNotCompleted(long v)
            {
                var notCompleted = Interlocked.Add(ref NotCompleted, v);
                OnRunningTaskCountChanged(new Tuple<long, long>(ThreadCount, notCompleted));
            }
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
                RegisterProgressChange(subtask, ProgressType.File);
                RegisterProgressChange(subtask, ProgressType.Folder);
                RegisterProgressChange(subtask, ProgressType.Size);
                RegisterProgressChange(subtask, ProgressType.LocalSearch);
                RegisterProgressChange(subtask, ProgressType.RunningTaskCount);
                this.Pausing += delegate { subtask.Pause(); };
            }
            Subtask AddAndGetSubtask(File.Uploader uploader)
            {
                Action<Tuple<long, long>> fileProgressCall = null, folderProgressCall, sizeProgressCall, localSearchStatusCall, runningTaskCountCall;
                var subtask = new Subtask(
                    new Func<Task>(async () =>await uploader.StartAsync()),
                    new Action(() => { uploader.Pause(); }),
                    out fileProgressCall, out folderProgressCall, out sizeProgressCall, out localSearchStatusCall, out runningTaskCountCall);
                uploader.Started += () =>
                {
                    subtask.OnStarted();
                    AddThreadCount(1);
                };
                uploader.Completed += (sender,success) =>
                {
                    subtask.OnCompleted(success);
                    AddThreadCount(-1);
                    if (success)
                    {
                        fileProgressCall(new Tuple<long, long>(1, 1));
                        AddNotCompleted(-1);
                    }
                };
                uploader.ErrorLogged += (msg) => OnErrorLogged(msg);
                AddNotCompleted(1);
                uploader.ProgressChanged += (p) => { sizeProgressCall(p); };
                AddSubtask(subtask);
                fileProgressCall(new Tuple<long, long>(0, 1));
                return subtask;
            }
            Subtask AddAndGetSubtask(Folder.Uploader uploader)
            {
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall, localSearchStatusCall, runningTaskCountCall;
                var subtask = new Subtask(
                    new Func<Task>(async () =>await uploader.StartAsync()),
                    new Action(() => { uploader.Pause(); }),
                    out fileProgressCall, out folderProgressCall, out sizeProgressCall, out localSearchStatusCall, out runningTaskCountCall);
                uploader.Started += () =>
                {
                    subtask.OnStarted();
                    AddThreadCount(1);
                };
                uploader.Completed += (sender,success) =>
                {
                    subtask.OnCompleted(success);
                    AddThreadCount(-1);
                    if (success) AddNotCompleted(-1);
                };
                uploader.ErrorLogged += (msg) => OnErrorLogged(msg);
                AddNotCompleted(1);
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
            private async Task CreateFolderTask()
            {
                if (Interlocked.CompareExchange(ref IsCreateFolderTaskInProgress, 1, 0) == 1)
                {
                    this.Debug($"{Constants.Icons.Warning} CreateFolderTask still busy, please wait...");
                    return;
                }
                try
                {
                    try
                    {
                        if (IsPausing) return;
                        if (0 == CreateFolderTaskProgress)
                        {
                            AddNotCompleted(1);
                            folderCreator.SetFolderMetadata(async (metadata) =>
                            {
                                metadata.name = F.Name;
                                metadata.createdTime = await F.GetTimeCreatedAsync();
                                metadata.modifiedTime = await F.GetTimeModifiedAsync();
                                return this.metadataFunc == null ? metadata : await this.metadataFunc(metadata);
                            });
                            this.Pausing += delegate { folderCreator.Stop(); };
                            this.FolderProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFolder, Interlocked.Increment(ref this.ProgressTotalFolder)));
                            AddNotCompleted(-1);
                            Interlocked.Increment(ref CreateFolderTaskProgress);
                            this.Debug($"{Constants.Icons.SubtaskCompleted} Folder \"{F.Name}\" is ready to be created");
                        }
                    }
                    finally { AddThreadCount(-1); }
                    if (IsPausing) return;
                    if (1 == CreateFolderTaskProgress)
                    {
                        AddThreadCount(1);
                        try
                        {
                            this.Debug($"{Constants.Icons.Hourglass} Creating folder...");
                            if (await folderCreator.StartAsync())
                            {
                                AddNotCompleted(-1);
                                this.FolderProgressChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref this.ProgressCurrentFolder), this.ProgressTotalFolder));
                                Interlocked.Increment(ref CreateFolderTaskProgress);
                                this.Debug($"{Constants.Icons.SubtaskCompleted} Folder created");
                            }
                            else this.Debug($"{Constants.Icons.Info} Folder create paused or failed");
                        }
                        finally { AddThreadCount(-1); }
                    }
                }
                finally { MyLogger.Assert(Interlocked.CompareExchange(ref IsCreateFolderTaskInProgress, 0, 1) == 1); }
            }
            int UploadSubfoldersTaskProgress = 0;
            int IsUploadSubfoldersTaskInProgress = 0;
            List<Subtask> UploadSubfoldersSubtasks = null;
            private async Task UploadSubfoldersTask()
            {
                if (Interlocked.CompareExchange(ref IsUploadSubfoldersTaskInProgress, 1, 0) == 1)
                {
                    this.Debug($"{Constants.Icons.Warning} UploadSubfoldersTask still busy, please wait...");
                    return;
                }
                try
                {
                    if (IsPausing) return;
                    if (0 == UploadSubfoldersTaskProgress)
                    {
                        this.Debug($"{Constants.Icons.Magnifier} Searching subfolders...");
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                        var subfolders = await F.GetFoldersAsync();
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Decrement(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                        this.Debug($"{Constants.Icons.Magnifier} Found {recordedSubfolderCount = subfolders.Count} subfolders");
                        UploadSubfoldersSubtasks = subfolders.Select((f) =>
                         {
                             var uploader = new Folder.Uploader(this,f);
                             uploader.folderCreator.SetFolderMetadata(async (metadata) =>
                             {
                                 var cloudId = await folderCreator.GetCloudId();
                                 if (cloudId == null) return null;
                                 metadata.parents = new List<string> { cloudId };
                                 return metadata;
                             });
                             return AddAndGetSubtask(uploader);
                         }).ToList();
                        AddNotCompleted(-1);
                        Interlocked.Increment(ref UploadSubfoldersTaskProgress);
                    }
                }
                finally
                {
                    AddThreadCount(-1);
                    MyLogger.Assert(Interlocked.CompareExchange(ref IsUploadSubfoldersTaskInProgress, 0, 1) == 1);
                }
                if (IsPausing) return;
                if (1 == UploadSubfoldersTaskProgress)
                {
                    this.Debug($"{Constants.Icons.Upload} Uploading subfolders...");
                    await Libraries.MyTask.WhenAll(UploadSubfoldersSubtasks.Select(async (subtask) =>
                    {
                        await Task.Delay(100);
                        if (this.IsActive) await subtask.Start();
                    }));
                    this.Debug($"{Constants.Icons.SubtaskCompleted} Subfolders upload completed or paused");
                }
            }
            int UploadSubfilesTaskProgress = 0;
            int IsUploadSubfilesTaskInProgress = 0;
            List<Subtask> UploadSubfilesSubtasks = null;
            private async Task UploadSubfilesTask()
            {
                if (Interlocked.CompareExchange(ref IsUploadSubfilesTaskInProgress, 1, 0) == 1)
                {
                    this.Debug($"{Constants.Icons.Warning} UploadSubfilesTask still busy, please wait...");
                    return;
                }
                try
                {
                    if (IsPausing) return;
                    if (0 == UploadSubfilesTaskProgress)
                    {
                        this.Debug($"{Constants.Icons.Magnifier} Searching subfiles...");
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Increment(ref this.SearchLocalFilesActions)));
                        var subfiles = await F.GetFilesAsync();
                        LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Add(ref this.SearchLocalFilesActions, -1 + subfiles.Count)));
                        this.Debug($"{Constants.Icons.Magnifier} Found {recordedSubfileCount = subfiles.Count} subfiles");
                        UploadSubfilesSubtasks =(await Libraries.MyTask.WhenAll(subfiles.Select(async (f) =>
                        {
                            var uploader = await f.GetUploader();
                            uploader.SetFileMetadata(async (metadata) =>
                            {
                                var cloudId = await folderCreator.GetCloudId();
                                if (cloudId == null) return null;
                                metadata.parents = new List<string> { cloudId };
                                return metadata;
                            });
                            var subtask = AddAndGetSubtask(uploader);
                            await uploader.GetFileSizeFirstAsync();
                            LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Decrement(ref this.SearchLocalFilesActions)));
                            return subtask;
                        }))).ToList();
                        AddNotCompleted(-1);
                        Interlocked.Increment(ref UploadSubfilesTaskProgress);
                    }
                }
                finally
                {
                    AddThreadCount(-1);
                    MyLogger.Assert(Interlocked.CompareExchange(ref IsUploadSubfilesTaskInProgress, 0, 1) == 1);
                }
                if (IsPausing) return;
                if (1 == UploadSubfilesTaskProgress)
                {
                    this.Debug($"{Constants.Icons.Upload} Uploading subfiles...");
                    await Libraries.MyTask.WhenAll(UploadSubfilesSubtasks.Select(async (subtask) =>
                    {
                        await Task.Delay(100);
                        if (this.IsActive) await subtask.Start();
                    }));
                    this.Debug($"{Constants.Icons.SubtaskCompleted} Subfiles upload completed or paused");
                }
            }
        }
    }
}
