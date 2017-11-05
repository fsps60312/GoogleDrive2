using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GoogleDrive2.Local
{
    partial class Folder
    {
        public partial class Uploader:Api.AdvancedApiOperator
        {
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> FileProgressChanged, FolderProgressChanged, SizeProgressChanged;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> RunningTaskCountChanged;
            Api.Files.FullCloudFileMetadata.FolderCreate folderCreator = new Api.Files.FullCloudFileMetadata.FolderCreate();
            public Folder F { get; private set; }
            public Uploader(Folder folder)
            {
                F = folder;
                folderCreator.SetFolderMetadata(async(metadata) =>
                {
                    metadata.name = F.Name;
                    metadata.createdTime =await F.GetTimeCreatedAsync();
                    metadata.modifiedTime = await F.GetTimeModifiedAsync();
                    return metadata;
                });
            }
            long ThreadCount = 0, NotCompleted = 0;
            long ProgressCurrentFile = 0, ProgressTotalFile = 0;
            long ProgressCurrentFolder = 0, ProgressTotalFolder = 0;
            long ProgressCurrentSize = 0, ProgressTotalSize = 0;
            List<Subtask> Subtasks;
            Tuple<long,long> MaintainProgress(Tuple<long,long>p,ref long current,ref long total,ref long parentCurrent,ref long parentTotal)
            {
                var cdif = p.Item1 - Interlocked.Exchange(ref current, p.Item1);
                var tdif = p.Item2 - Interlocked.Exchange(ref total, p.Item2);
                return new Tuple<long, long>(
                    Interlocked.Add(ref ProgressCurrentFile, cdif),
                    Interlocked.Add(ref ProgressTotalFile, tdif));
            }
            void MaintainSubtask(Subtask subtask)
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
                    var threadCount=Interlocked.Decrement(ref ThreadCount);
                    var notCompleted = Interlocked.Decrement(ref NotCompleted);
                    RunningTaskCountChanged?.Invoke(new Tuple<long, long>(threadCount, notCompleted));
                    if (notCompleted == 0) this.OnCompleted(true);
                };
                {
                    long current = 0, total = 0;
                    subtask.FileProgressChanged += (p) =>
                    {
                        this.FileProgressChanged?.Invoke(MaintainProgress(p,
                            ref current, ref total, ref ProgressCurrentFile, ref ProgressTotalFile));
                    };
                }
                {
                    long current = 0, total = 0;
                    subtask.FolderProgressChanged += (p) =>
                    {
                        this.FileProgressChanged?.Invoke(MaintainProgress(p,
                            ref current, ref total, ref ProgressCurrentFolder, ref ProgressTotalFolder));
                    };
                }
                {
                    long current = 0, total = 0;
                    subtask.SizeProgressChanged += (p) =>
                    {
                        this.FileProgressChanged?.Invoke(MaintainProgress(p,
                            ref current, ref total, ref ProgressCurrentSize, ref ProgressTotalSize));
                    };
                }
                this.Started += async () => { await subtask.Start(); };
                this.Pausing += () => { subtask.Pause(); };
                Subtasks.Add(subtask);
            }
            Subtask GetSubtask(File.Uploader uploader)
            {
                Action pausedCall;
                Action<bool> completedCall;
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall;
                var subtask = new Subtask(
                    new Func<Task>(async () => { await uploader.StartAsync(); }),
                    new Action(() => { uploader.Pause(); }),
                    out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall);
                uploader.Paused += () => { pausedCall(); };
                uploader.Completed += (success) =>
                {
                    completedCall(success);
                    fileProgressCall(new Tuple<long, long>(1, 1));
                };
                uploader.ProgressChanged += (p) => { sizeProgressCall(p); };
                MaintainSubtask(subtask);
                fileProgressCall(new Tuple<long, long>(0, 1));
                return subtask;
            }
            Subtask GetSubtask(Folder.Uploader uploader)
            {
                Action pausedCall;
                Action<bool> completedCall;
                Action<Tuple<long, long>> fileProgressCall, folderProgressCall, sizeProgressCall;
                var subtask = new Subtask(
                    new Func<Task>(async () => { await uploader.StartAsync(); }),
                    new Action(() => { uploader.Pause(); }),
                    out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall);
                uploader.Paused += () => { pausedCall(); };
                uploader.Completed += (success) => { completedCall(success); };
                uploader.FileProgressChanged += (p) => { fileProgressCall(p); };
                uploader.FolderProgressChanged += (p) => { folderProgressCall(p); };
                uploader.SizeProgressChanged += (p) => { sizeProgressCall(p); };
                MaintainSubtask(subtask);
                return subtask;
            }
            private async Task UploadSubfoldersTask()
            {
                this.Debug("Searching subfolders...");
                var subfolders = await F.GetFoldersAsync();
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
                    var subtask = GetSubtask(uploader);
                    Interlocked.Add(ref NotCompleted, -1);
                    if (this.IsActive) await subtask.Start();
                }));
            }
            private async Task UploadSubfilesTask()
            {
                this.Debug("Searching subfiles...");
                var subfiles = await F.GetFilesAsync();
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
                    var subtask = GetSubtask(uploader);
                    Interlocked.Add(ref NotCompleted, -1);
                    if (this.IsActive) await subtask.Start();
                }));
            }
            protected override async Task StartPrivateAsync()
            {
                //TODO: NotCompleted
                await Task.WhenAll(new Task[]{
                    folderCreator.StartAsync(),
                    UploadSubfoldersTask(),
                    UploadSubfilesTask()
                });
            }
        }
    }
}
