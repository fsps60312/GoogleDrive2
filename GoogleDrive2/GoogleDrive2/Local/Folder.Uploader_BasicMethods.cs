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
        public partial class Uploader :Libraries.MyWrappedTasks
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
            void RegisterProgressChange(Folder.Uploader uploader, ProgressType progressType)
            {
                long current = 0, total = 0;
                switch (progressType)
                {
                    case ProgressType.File:
                        uploader.FileProgressChanged += (p) =>
                        {
                            this.FileProgressChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref ProgressCurrentFile, ref ProgressTotalFile));
                        };
                        break;
                    case ProgressType.Folder:
                        uploader.FolderProgressChanged += (p) =>
                        {
                            this.FolderProgressChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref ProgressCurrentFolder, ref ProgressTotalFolder));
                        };
                        break;
                    case ProgressType.Size:
                        uploader.SizeProgressChanged += (p) =>
                        {
                            this.SizeProgressChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref ProgressCurrentSize, ref ProgressTotalSize));
                        };
                        break;
                    case ProgressType.LocalSearch:
                        uploader.LocalSearchStatusChanged += (p) =>
                        {
                            this.LocalSearchStatusChanged?.Invoke(MaintainProgress(p,
                                ref current, ref total, ref SearchLocalFoldersActions, ref SearchLocalFilesActions));
                        };
                        break;
                    case ProgressType.RunningTaskCount:
                        uploader.RunningTaskCountChanged += (p) =>
                        {
                            this.OnRunningTaskCountChanged(MaintainProgress(p,
                                ref current, ref total, ref ThreadCount, ref NotCompleted));
                        };
                        break;
                    default: MyLogger.LogError($"Unexpected progressType: {progressType}"); break;
                }
            }
            private Task AddCreateFolderTask()
            {
                folderCreator.SetFolderMetadata(async (metadata) =>
                {
                    metadata.name = F.Name;
                    metadata.createdTime = await F.GetTimeCreatedAsync();
                    metadata.modifiedTime = await F.GetTimeModifiedAsync();
                    return this.metadataFunc == null ? metadata : await this.metadataFunc(metadata);
                });
                this.FolderProgressChanged?.Invoke(new Tuple<long, long>(this.ProgressCurrentFolder, Interlocked.Increment(ref this.ProgressTotalFolder)));
                folderCreator.Started += delegate { AddThreadCount(1); };
                folderCreator.Unstarted += delegate { AddThreadCount(-1); };
                folderCreator.Completed += delegate
                {
                    this.FolderProgressChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref this.ProgressCurrentFolder), this.ProgressTotalFolder));
                    this.Debug($"{Constants.Icons.SubtaskCompleted} Folder created");
                    AddNotCompleted(-1);
                };
                AddNotCompleted(1);
                this.Debug($"{Constants.Icons.SubtaskCompleted} Folder \"{F.Name}\" is ready to be created");
                this.AddSubTask(folderCreator);
                return Task.CompletedTask;
            }
            void RegisterFolderUploader(Folder.Uploader uploader)
            {
                uploader.Started += delegate { AddThreadCount(1); };
                uploader.Unstarted += delegate { AddThreadCount(-1); };
                uploader.Completed += delegate { AddNotCompleted(-1); };
                uploader.ErrorLogged += (msg) => OnErrorLogged(msg);
                AddNotCompleted(1);
                foreach (var type in Enum.GetValues(typeof(ProgressType)))
                {
                    RegisterProgressChange(uploader, (ProgressType)type);
                }
            }
            void RegisterFileUploader(File.Uploader uploader)
            {
                long currentFile = 0, totalFile = 0, currentSize = 0, totalSize = 0;
                var fileProgressCall = new Action<Tuple<long, long>>((p) =>
                   {
                       this.FileProgressChanged?.Invoke(MaintainProgress(p,
                           ref currentFile, ref totalFile, ref ProgressCurrentFile, ref ProgressTotalFile));
                   });
                var sizeProgressCall = new Action<Tuple<long, long>>((p) =>
                   {
                       this.SizeProgressChanged?.Invoke(MaintainProgress(p,
                           ref currentSize, ref totalSize, ref ProgressCurrentSize, ref ProgressTotalSize));
                   });
                uploader.Started += delegate { AddThreadCount(1); };
                uploader.Unstarted += delegate { AddThreadCount(-1); };
                uploader.Completed += delegate
                  {
                      fileProgressCall(new Tuple<long, long>(1, 1));
                      AddNotCompleted(-1);
                  };
                uploader.ErrorLogged += (msg) => OnErrorLogged(msg);
                AddNotCompleted(1);
                uploader.ProgressChanged += (p) => { sizeProgressCall(p); };
                fileProgressCall(new Tuple<long, long>(0, 1));
            }
            private async Task AddUploadSubfoldersTasks()
            {
                this.Debug($"{Constants.Icons.Magnifier} Searching subfolders...");
                LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Increment(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                var subfolders = await F.GetFoldersAsync();
                LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Interlocked.Decrement(ref SearchLocalFoldersActions), this.SearchLocalFilesActions));
                this.Debug($"{Constants.Icons.Magnifier} Found {subfolders.Count} subfolders");
                foreach (var uploader in subfolders.Select((f) =>
                {
                    var uploader = new Folder.Uploader(this, f);
                    uploader.folderCreator.SetFolderMetadata(async (metadata) =>
                    {
                        var cloudId = await folderCreator.GetCloudId();
                        if (cloudId == null) return null;
                        metadata.parents = new List<string> { cloudId };
                        return metadata;
                    });
                    RegisterFolderUploader(uploader);
                    return uploader;
                }))
                {
                    AddSubTask(uploader);
                }
            }
            private async Task AddUploadSubfilesTasks()
            {
                this.Debug($"{Constants.Icons.Magnifier} Searching subfiles...");
                LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Increment(ref this.SearchLocalFilesActions)));
                var subfiles = await F.GetFilesAsync();
                LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Add(ref this.SearchLocalFilesActions, -1 + subfiles.Count)));
                this.Debug($"{Constants.Icons.Magnifier} Found {subfiles.Count} subfiles");
                foreach (var uploader in await Task.WhenAll(subfiles.Select(async (f) =>
                {
                    var uploader = await f.GetUploader();
                    uploader.SetFileMetadata(async (metadata) =>
                    {
                        var cloudId = await folderCreator.GetCloudId();
                        if (cloudId == null) return null;
                        metadata.parents = new List<string> { cloudId };
                        return metadata;
                    });
                    LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(SearchLocalFoldersActions, Interlocked.Decrement(ref this.SearchLocalFilesActions)));
                    RegisterFileUploader(uploader);
                    await uploader.GetFileSizeFirstAsync();
                    return uploader;
                })))
                {
                    AddSubTask(uploader);
                }
            }
        }
    }
}
