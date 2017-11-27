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
        public partial class Uploader : Libraries.MyWrappedTasks
        {
            class MySet<T>
            {
                object syncRoot = new object();
                SortedSet<T> data = new SortedSet<T>();
                public void Add(T v)
                {
                    lock (syncRoot) data.Add(v);
                }
                public T Get(int index)
                {
                    lock (syncRoot) return data.ElementAt(index);
                }
                public void Remove(int index)
                {
                    lock (syncRoot) data.Remove(this.Get(index));
                }
                public void SetComparer(IComparer<T> comparer)
                {
                    lock (syncRoot)
                    {
                        var preData = data;
                        data = new SortedSet<T>(comparer);
                        data.UnionWith(preData);
                    }
                }
                public void Clear()
                {
                    lock (syncRoot) data.Clear();
                }
            }

            //Libraries.FrequentExecutionLimiter
            //    FileProgressChangedLimiter = new Libraries.FrequentExecutionLimiter(0.2),
            //    FolderProgressChangedLimiter = new Libraries.FrequentExecutionLimiter(0.2),
            //    SizeProgressChangedLimiter = new Libraries.FrequentExecutionLimiter(0.2),
            //    SearchProgressChangedLimiter = new Libraries.FrequentExecutionLimiter(0.2),
            //    ThreadProgressChangedLimiter = new Libraries.FrequentExecutionLimiter(0.2);
            long ProgressCurrentFile = 0, ProgressTotalFile = 0;
            long ProgressCurrentFolder = 0, ProgressTotalFolder = 0;
            long ProgressCurrentSize = 0, ProgressTotalSize = 0;
            long SearchLocalFoldersActions = 0, SearchLocalFilesActions = 0;
            long ThreadCount = 0, NotCompleted = 0;
            void Add(ref long v, long addv) { Interlocked.Add(ref v, addv); }
            void Increment(ref long v) { Interlocked.Increment(ref v); }
            void Decrement(ref long v) { Interlocked.Decrement(ref v); }
            long Exchange(ref long a, long b) { return Interlocked.Exchange(ref a, b); }
            long Read(ref long v) { return Interlocked.Read(ref v); }
            void TriggerEvent(ProgressType progressType)
            {
                switch (progressType)
                {
                    case ProgressType.File: FileProgressChanged?.Invoke(new Tuple<long, long>(Read(ref ProgressCurrentFile), Read(ref ProgressTotalFile))); return;
                    case ProgressType.Folder: FolderProgressChanged?.Invoke(new Tuple<long, long>(Read(ref ProgressCurrentFolder), Read(ref ProgressTotalFolder))); return;
                    case ProgressType.Size: SizeProgressChanged?.Invoke(new Tuple<long, long>(Read(ref ProgressCurrentSize), Read(ref ProgressTotalSize))); return;
                    case ProgressType.LocalSearch: LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(Read(ref SearchLocalFoldersActions), Read(ref SearchLocalFilesActions))); return;
                    case ProgressType.RunningTaskCount: RunningTaskCountChanged?.Invoke(new Tuple<long, long>(Read(ref ThreadCount), Read(ref NotCompleted))); return;
                    default: MyLogger.LogError($"Unexpected progressType: {progressType}"); break;
                }
            }
            //void OnFileProgressChanged(long c,long t)
            //{
            //    FileProgressChangedLimiter.Execute(() => FileProgressChanged?.Invoke(new Tuple<long, long>(c, t)));
            //}
            //void OnFolderProgressChanged(long c, long t)
            //{
            //    FolderProgressChangedLimiter.Execute(() => FolderProgressChanged?.Invoke(new Tuple<long, long>(c, t)));
            //}
            //void OnSizeProgressChanged(long c, long t)
            //{
            //    SizeProgressChangedLimiter.Execute(() => SizeProgressChanged?.Invoke(new Tuple<long, long>(c, t)));
            //}
            //void OnSearchProgressChanged(long c, long t)
            //{
            //    SearchProgressChangedLimiter.Execute(() => LocalSearchStatusChanged?.Invoke(new Tuple<long, long>(c, t)));
            //}
            //void OnThreadProgressChanged(long c, long t)
            //{
            //    ThreadProgressChangedLimiter.Execute(() => RunningTaskCountChanged?.Invoke(new Tuple<long, long>(c, t)));
            //}
            //long __ProgressCurrentFile__ = 0, __ProgressTotalFile__ = 0;
            //long __ProgressCurrentFolder__ = 0, __ProgressTotalFolder__ = 0;
            //long __ProgressCurrentSize__ = 0, __ProgressTotalSize__ = 0;
            //long __SearchLocalFoldersActions__ = 0, __SearchLocalFilesActions__ = 0;
            //long __ThreadCount__ = 0, __NotCompleted__ = 0;
            //long ProgressCurrentFile
            //{
            //    get { return __ProgressCurrentFile__; }
            //    set { OnFileProgressChanged(__ProgressCurrentFile__ = value, ProgressTotalFile); }
            //}
            //long ProgressTotalFile
            //{
            //    get { return __ProgressTotalFile__; }
            //    set { OnFileProgressChanged(ProgressCurrentFile, __ProgressTotalFile__ = value); }
            //}
            //long ProgressCurrentFolder
            //{
            //    get { return __ProgressCurrentFolder__; }
            //    set { OnFolderProgressChanged(__ProgressCurrentFolder__ = value, ProgressTotalFolder); }
            //}
            //long ProgressTotalFolder
            //{
            //    get { return __ProgressTotalFolder__; }
            //    set { OnFolderProgressChanged(ProgressCurrentFolder, __ProgressTotalFolder__ = value); }
            //}
            //long ProgressCurrentSize
            //{
            //    get { return __ProgressCurrentSize__; }
            //    set { OnSizeProgressChanged(__ProgressCurrentSize__ = value, ProgressTotalSize); }
            //}
            //long ProgressTotalSize
            //{
            //    get { return __ProgressTotalSize__; }
            //    set { OnSizeProgressChanged(ProgressCurrentSize, __ProgressTotalSize__ = value); }
            //}
            //long SearchLocalFoldersActions
            //{
            //    get { return __SearchLocalFoldersActions__; }
            //    set { OnSearchProgressChanged(__SearchLocalFoldersActions__ = value, SearchLocalFilesActions); }
            //}
            //long SearchLocalFilesActions
            //{
            //    get { return __SearchLocalFilesActions__; }
            //    set { OnSearchProgressChanged(SearchLocalFoldersActions, __SearchLocalFilesActions__ = value); }
            //}
            //long ThreadCount
            //{
            //    get { return __ThreadCount__; }
            //    set { OnThreadProgressChanged(__ThreadCount__ = value, NotCompleted); }
            //}
            //long NotCompleted
            //{
            //    get { return __NotCompleted__; }
            //    set { OnThreadProgressChanged(ThreadCount, __NotCompleted__ = value); }
            //}
            enum ProgressType { File, Folder, Size, LocalSearch, RunningTaskCount };
            void MaintainProgress(Tuple<long, long> p, ref long current, ref long total, ProgressType progressType)
            {
                var cdif = p.Item1 - Exchange(ref current, p.Item1);
                var tdif = p.Item2 - Exchange(ref total, p.Item2);
                switch (progressType)
                {
                    case ProgressType.File:
                        Add(ref ProgressCurrentFile, cdif);
                        Add(ref ProgressTotalFile, tdif);
                        break;
                    case ProgressType.Folder:
                        Add(ref ProgressCurrentFolder, cdif);
                        Add(ref ProgressTotalFolder, tdif);
                        break;
                    case ProgressType.Size:
                        Add(ref ProgressCurrentSize, cdif);
                        Add(ref ProgressTotalSize, tdif);
                        break;
                    case ProgressType.LocalSearch:
                        Add(ref SearchLocalFoldersActions, cdif);
                        Add(ref SearchLocalFilesActions, tdif);
                        break;
                    case ProgressType.RunningTaskCount:
                        Add(ref ThreadCount, cdif);
                        Add(ref NotCompleted, tdif);
                        break;
                    default: MyLogger.LogError($"Unexpected progressType: {progressType}"); break;
                }
                TriggerEvent(progressType);
            }
            void RegisterProgressChange(Folder.Uploader uploader, ProgressType progressType)
            {
                long current = 0, total = 0;
                switch (progressType)
                {
                    case ProgressType.File:
                        uploader.FileProgressChanged += (p) =>
                        {
                            MaintainProgress(p, ref current, ref total, progressType);
                        };
                        break;
                    case ProgressType.Folder:
                        uploader.FolderProgressChanged += (p) =>
                        {
                            MaintainProgress(p, ref current, ref total, progressType);
                        };
                        break;
                    case ProgressType.Size:
                        uploader.SizeProgressChanged += (p) =>
                        {
                            MaintainProgress(p, ref current, ref total, progressType);
                        };
                        break;
                    case ProgressType.LocalSearch:
                        uploader.LocalSearchStatusChanged += (p) =>
                        {
                            MaintainProgress(p, ref current, ref total, progressType);
                        };
                        break;
                    case ProgressType.RunningTaskCount:
                        uploader.RunningTaskCountChanged += (p) =>
                        {
                            MaintainProgress(p, ref current, ref total, progressType);
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
                Increment(ref ProgressTotalFolder); TriggerEvent(ProgressType.Folder);
                folderCreator.Started += delegate { Increment(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                folderCreator.Unstarted += delegate { Decrement(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                folderCreator.Completed += delegate
                {
                    Increment(ref ProgressCurrentFolder); TriggerEvent(ProgressType.Folder);
                    this.Debug($"{Constants.Icons.SubtaskCompleted} Folder created");
                    Decrement(ref NotCompleted); TriggerEvent(ProgressType.RunningTaskCount);
                };
                Increment(ref NotCompleted); TriggerEvent(ProgressType.RunningTaskCount);
                this.Debug($"{Constants.Icons.SubtaskCompleted} Folder \"{F.Name}\" is ready to be created");
                this.AddSubTask(folderCreator);
                return Task.CompletedTask;
            }
            void RegisterFolderUploader(Folder.Uploader uploader)
            {
                uploader.Started += delegate { Increment(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.ExtraThreadWaited += delegate { Decrement(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.ExtraThreadReleased += delegate { Increment(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.Unstarted += delegate { Decrement(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.Completed += delegate { Decrement(ref NotCompleted); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.ErrorLogged += (msg) => OnErrorLogged(msg);
                Increment(ref NotCompleted); TriggerEvent(ProgressType.RunningTaskCount);
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
                    MaintainProgress(p, ref currentFile, ref totalFile, ProgressType.File);
                });
                var sizeProgressCall = new Action<Tuple<long, long>>((p) =>
                {
                    MaintainProgress(p, ref currentSize, ref totalSize, ProgressType.Size);
                });
                uploader.Started += delegate { Increment(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.Unstarted += delegate { Decrement(ref ThreadCount); TriggerEvent(ProgressType.RunningTaskCount); };
                uploader.Completed += delegate
                  {
                      fileProgressCall(new Tuple<long, long>(1, 1));
                      Decrement(ref NotCompleted); TriggerEvent(ProgressType.RunningTaskCount);
                  };
                uploader.ErrorLogged += (msg) => OnErrorLogged(msg);
                Increment(ref NotCompleted); TriggerEvent(ProgressType.RunningTaskCount);
                uploader.ProgressChanged += (p) => { sizeProgressCall(p); };
                fileProgressCall(new Tuple<long, long>(0, 1));
            }
            private async Task AddUploadSubfoldersTasks()
            {
                this.Debug($"{Constants.Icons.Magnifier} Searching subfolders...");
                Increment(ref SearchLocalFoldersActions); TriggerEvent(ProgressType.LocalSearch);
                var subfolders = await F.GetFoldersAsync();
                Decrement(ref SearchLocalFoldersActions); TriggerEvent(ProgressType.LocalSearch);
                this.Debug($"{Constants.Icons.Magnifier} Found {subfolders.Count} subfolders");
                foreach (var uploader in subfolders.Select((f) =>
                {
                    var uploader = new Folder.Uploader(this, f);
                    uploader.folderCreator.SetFolderMetadata(async (metadata) =>
                    {
                        var cloudId = await folderCreator.GetCloudId(uploader.folderCreator.CancellationToken);
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
                Increment(ref SearchLocalFilesActions); TriggerEvent(ProgressType.LocalSearch);
                var subfiles = await F.GetFilesAsync();
                Add(ref SearchLocalFilesActions, subfiles.Count - 1); TriggerEvent(ProgressType.LocalSearch);
                this.Debug($"{Constants.Icons.Magnifier} Found {subfiles.Count} subfiles");
                foreach (var uploader in await Task.WhenAll(subfiles.Select(async (f) =>
                {
                    var uploader = await f.GetUploader();
                    uploader.SetFileMetadata(async (metadata) =>
                    {
                        var cloudId = await folderCreator.GetCloudId(uploader.CancellationToken);
                        if (cloudId == null) return null;
                        metadata.parents = new List<string> { cloudId };
                        return metadata;
                    });
                    Decrement(ref SearchLocalFilesActions); TriggerEvent(ProgressType.LocalSearch);
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