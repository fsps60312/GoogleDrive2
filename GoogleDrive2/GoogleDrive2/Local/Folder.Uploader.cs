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
                folderCreator.SetFolderMetadata(async (metadata) =>
                {
                    metadata.name = F.Name;
                    metadata.createdTime = await F.GetTimeCreatedAsync();
                    metadata.modifiedTime = await F.GetTimeModifiedAsync();
                    return metadata;
                });
                folderCreator.Completed += (success) =>
                {
                    if (success)
                    {
                        Interlocked.Increment(ref CreateFolderTaskProgress);
                        this.FolderProgressChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref this.ProgressCurrentFolder), this.ProgressTotalFolder));
                    }
                };
                NewUploaderCreated?.Invoke(this);
            }
            Api.Files.FullCloudFileMetadata.FolderCreate folderCreator = new Api.Files.FullCloudFileMetadata.FolderCreate();
            long ThreadCount = 0, NotCompleted = 0;
            protected override async Task StartPrivateAsync()
            {
                if (CheckPause()) return;
                this.SizeProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentSize, this.ProgressTotalSize));
                this.FileProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFile, this.ProgressTotalFile));
                this.FolderProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFolder, this.ProgressTotalFolder));
                var tasks = new Task[]{
                    CreateFolderTask(),
                    UploadSubfoldersTask(),
                    UploadSubfilesTask()
                };
                Interlocked.Add(ref ThreadCount, tasks.Length);
                await Task.WhenAll(tasks.Select(async(t)=>
                {
                    try { await t; }
                    finally { if (Interlocked.Decrement(ref ThreadCount) == 0) CheckPause(); }
                }));
            }
        }
    }
}
