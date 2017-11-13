using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GoogleDrive2.Local
{
    partial class Folder
    {
        public partial class Uploader : Api.AdvancedApiOperator
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
            bool ShouldReturn(bool? v, out bool result)
            {
                if (v.HasValue)
                {
                    result = v.Value;
                    return true;
                }
                else return result = false;//just to initialize "result"
            }
            bool? MergeResults(bool?[] status)
            {
                var s = status.SelectMany((v) => { return v.HasValue ? new bool?[] { v } : new bool?[] { }; }).ToArray();
                MyLogger.Assert(s.Length <= 1);//Should not return twice
                return s.Length == 0 ? null : s[0];
            }
            int recordedSubfileCount = -1, recordedSubfolderCount = -1;
            int StartPrivateAsyncProgress = 0;
            protected override async Task<bool> StartPrivateAsync()
            {
                if (CheckPause()) return false;
                if (0 == StartPrivateAsyncProgress)
                {
                    //Interlocked.Add(ref AddedThreadCount, 3);
                    AddNotCompleted(3);
                    Interlocked.Increment(ref StartPrivateAsyncProgress);
                }
                if (CheckPause()) return false;
                if (1 == StartPrivateAsyncProgress)
                {
                    AddThreadCount(3);
                    await Task.WhenAll(new Task[]{
                            CreateFolderTask(),
                            UploadSubfoldersTask(),
                            UploadSubfilesTask()
                        });
                }
                MyLogger.Assert(ThreadCount == 0);
                return NotCompleted == 0;
            }
            static volatile int InstanceCount = 0;
            public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
            static void AddInstanceCount(int value) { InstanceCountChanged?.Invoke(Interlocked.Add(ref InstanceCount, value)); }
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