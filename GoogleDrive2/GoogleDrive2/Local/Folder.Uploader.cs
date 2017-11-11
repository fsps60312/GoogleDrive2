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
            public Uploader(Uploader parent, Folder folder)
            {
                Parent = parent;
                F = folder;
                AddNotCompleted(1);
                folderCreator = new Api.Files.FullCloudFileMetadata.FolderCreate();
                NewUploaderCreated?.Invoke(this);
            }
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
            protected override async Task<bool> StartPrivateAsync()
            {
                if (CheckPause()) return false;
                this.SizeProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentSize, this.ProgressTotalSize));
                this.FileProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFile, this.ProgressTotalFile));
                this.FolderProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFolder, this.ProgressTotalFolder));
                Interlocked.Add(ref AddedThreadCount, 3);
                AddThreadCount(3);
                var result = MergeResults(await Task.WhenAll(new Task[]{
                            CreateFolderTask(),
                            UploadSubfoldersTask(),
                            UploadSubfilesTask()
                        }.Select(new Func<Task, Task<bool?>>(async (t) =>
                        {
                            bool? ans;
                            try { await t; }
                            finally
                            {
                                Interlocked.Add(ref AddedThreadCount, -1);
                                ans = AddThreadCount(-1);
                            }
                            return ans;
                        }))));
                if (!result.HasValue)
                {
                    var msg = $"!result.HasValue.\r\n" +
                        $"ThreadCount={ThreadCount}, NotCompleted={NotCompleted}, AddedThreadCount={AddedThreadCount}\r\n" +
                        $"Progress=({CreateFolderTaskProgress},{UploadSubfoldersTaskProgress},{UploadSubfilesTaskProgress})\r\n" +
                        $"FileCount={recordedSubfileCount}, FolderCount={recordedSubfolderCount}";
                    this.LogError(msg);
                    //await MyLogger.Alert(msg);
                }
                return result.HasValue ? result.Value : false;
            }
        }
    }
}