using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GoogleDrive2.Local
{
    partial class Folder
    {
        public partial class Uploader
        {
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> FileProgressChanged, FolderProgressChanged, SizeProgressChanged, LocalSearchStatusChanged;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> RunningTaskCountChanged;
            public Folder F { get; private set; }
            public Uploader Parent { get; private set; }
            public int GetIndent() { return Parent == null ? 0 : Parent.GetIndent() + 1; }
            private Func<Api.Files.FullCloudFileMetadata, Task<Api.Files.FullCloudFileMetadata>> metadataFunc = null;
            public void SetFolderMetadata(Func<Api.Files.FullCloudFileMetadata, Task<Api.Files.FullCloudFileMetadata>> func) { metadataFunc = func; }
            Api.Files.FullCloudFileMetadata.FolderCreate folderCreator = null;
            int MainTaskProgress = 0;
            protected override async Task<bool> AddSubtasksIfNot()
            {
                if (IsPausing) return false;
                if (0 == MainTaskProgress)
                {
                    this.Debug($"{Constants.Icons.Progress} Step 1/3...");
                    await AddCreateFolderTask();
                    Interlocked.Increment(ref MainTaskProgress);
                }
                if (IsPausing) return false;
                if (1 == MainTaskProgress)
                {
                    this.Debug($"{Constants.Icons.Progress} Step 2/3...");
                    await AddUploadSubfilesTasks();
                    Interlocked.Increment(ref MainTaskProgress);
                }
                if (IsPausing) return false;
                if (2 == MainTaskProgress)
                {
                    this.Debug($"{Constants.Icons.Progress} Step 3/3...");
                    await AddUploadSubfoldersTasks();
                    Interlocked.Increment(ref MainTaskProgress);
                }
                return true;
            }
            static volatile int InstanceCount = 0;
            public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
            static void AddInstanceCount(int value) { System.Threading.Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
            ~Uploader() { AddInstanceCount(-1); }
            public Uploader(Uploader parent, Folder folder)
            {
                AddInstanceCount(1);
                Parent = parent;
                F = folder;
                folderCreator = new Api.Files.FullCloudFileMetadata.FolderCreate();
                NewUploaderCreated?.Invoke(this);
            }
        }
    }
}