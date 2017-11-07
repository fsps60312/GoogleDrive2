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
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> FileProgressChanged, FolderProgressChanged, SizeProgressChanged,LocalSearchStatusChanged;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> RunningTaskCountChanged;
            public Folder F { get; private set; }
            public void SetFolderMetadata(Func<Api.Files.FullCloudFileMetadata, Task<Api.Files.FullCloudFileMetadata>> func)
            {
                folderCreator.SetFolderMetadata(func);
            }
            public Uploader(Folder folder)
            {
                F = folder;
                AddNotCompleted(1);
                folderCreator.SetFolderMetadata(async (metadata) =>
                {
                    metadata.name = F.Name;
                    metadata.createdTime = await F.GetTimeCreatedAsync();
                    metadata.modifiedTime = await F.GetTimeModifiedAsync();
                    return metadata;
                });
                folderCreator.Completed += (success) =>
                {
                    AddThreadCount(-1);
                    if (success)
                    {
                        AddNotCompleted(-1);
                        Interlocked.Increment(ref CreateFolderTaskProgress);
                        this.FolderProgressChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref this.ProgressCurrentFolder), this.ProgressTotalFolder));
                    }
                };
                this.Pausing += () => { folderCreator.Stop(); };
                NewUploaderCreated?.Invoke(this);
            }
            Api.Files.FullCloudFileMetadata.FolderCreate folderCreator = new Api.Files.FullCloudFileMetadata.FolderCreate();
            protected override async Task StartPrivateAsync()
            {
                if (CheckPause()) return;
                this.SizeProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentSize, this.ProgressTotalSize));
                this.FileProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFile, this.ProgressTotalFile));
                this.FolderProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFolder, this.ProgressTotalFolder));
                Interlocked.Add(ref AddedThreadCount, 3);
                AddThreadCount(3);
                await Task.WhenAll(new Task[]{
                    CreateFolderTask(),
                    UploadSubfoldersTask(),
                    UploadSubfilesTask()
                }.Select(async (t) =>
                {
                    try { await t; }
                    finally
                    {
                        Interlocked.Add(ref AddedThreadCount, -1);
                        AddThreadCount(-1);
                    }
                }));
                //await Task.WhenAll(tasks.Select(async(t)=>
                //{
                //    try { await t; }
                //    finally
                //    {
                //        var threadCount = Interlocked.Decrement(ref ThreadCount);
                //        RunningTaskCountChanged?.Invoke(new Tuple<long, long>(threadCount, NotCompleted));
                //        if (threadCount == 0) CheckPause();
                //    }
                //}));
            }
        }
    }
}
