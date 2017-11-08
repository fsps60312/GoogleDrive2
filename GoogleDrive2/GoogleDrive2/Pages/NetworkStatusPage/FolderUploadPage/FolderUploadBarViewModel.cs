using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDrive2.Pages.NetworkStatusPage.FolderUploadPage
{
    class FolderUploadBarViewModel : NetworkStatusWithSpeedBarViewModel
    {
        #region Properties
        string __CurrentFile__ = $"{Constants.Icons.File}0";
        string __TotalFile__ = $"{Constants.Icons.File}0";
        string __CurrentFolder__ = null;
        string __TotalFolder__ = null;
        string __CurrentSize__ = null;
        string __TotalSize__ = null;
        double __FileProgress__ = 0;
        double __FolderProgress__ = 0;
        double __SizeProgress__ = 0;
        string __TaskStatus__ = null;
        string __SearchFoldersStatus__ = null;
        string __SearchFilesStatus__ = null;
        string __FileStatus__ = null;//SearchFilesStatus+CurrentFile
        string __FolderStatus__ = null;//SearchFoldersStatus+CurrentFolder
        string __CurrentFileAndSize__ = null;//CurrentFileAndSize
        public string CurrentFile
        {
            get { return __CurrentFile__; }
            set
            {
                if (value == __CurrentFile__) return;
                __CurrentFile__ = value;
                CurrentFileAndSize = $"{CurrentFile} | {CurrentSize}";
                OnPropertyChanged("CurrentFile");
            }
        }
        public string TotalFile
        {
            get { return __TotalFile__; }
            set
            {
                if (value == __TotalFile__) return;
                __TotalFile__ = value;
                FileStatus = TotalFile + (string.IsNullOrEmpty(SearchFilesStatus) ? "" : $"{SearchFilesStatus}");
                OnPropertyChanged("TotalFile");
            }
        }
        public string CurrentFolder
        {
            get { return __CurrentFolder__; }
            set
            {
                if (value == __CurrentFolder__) return;
                __CurrentFolder__ = value;
                OnPropertyChanged("CurrentFolder");
            }
        }
        public string TotalFolder
        {
            get { return __TotalFolder__; }
            set
            {
                if (value == __TotalFolder__) return;
                __TotalFolder__ = value;
                FolderStatus = TotalFolder + (string.IsNullOrEmpty(SearchFoldersStatus) ? "" : $"{SearchFoldersStatus}");
                OnPropertyChanged("TotalFolder");
            }
        }
        public string CurrentSize
        {
            get { return __CurrentSize__; }
            set
            {
                if (value == __CurrentSize__) return;
                __CurrentSize__ = value;
                CurrentFileAndSize = $"{CurrentFile} | {CurrentSize}";
                OnPropertyChanged("CurrentSize");
            }
        }
        public string TotalSize
        {
            get { return __TotalSize__; }
            set
            {
                if (value == __TotalSize__) return;
                __TotalSize__ = value;
                OnPropertyChanged("TotalSize");
            }
        }
        public double FileProgress
        {
            get { return __FileProgress__; }
            set
            {
                if (value == __FileProgress__) return;
                __FileProgress__ = value;
                OnPropertyChanged("FileProgress");
            }
        }
        public double FolderProgress
        {
            get { return __FolderProgress__; }
            set
            {
                if (value == __FolderProgress__) return;
                __FolderProgress__ = value;
                OnPropertyChanged("FolderProgress");
            }
        }
        public double SizeProgress
        {
            get { return __SizeProgress__; }
            set
            {
                if (value == __SizeProgress__) return;
                __SizeProgress__ = value;
                OnPropertyChanged("SizeProgress");
            }
        }
        public string TaskStatus
        {
            get { return __TaskStatus__; }
            set
            {
                if (value == __TaskStatus__) return;
                __TaskStatus__ = value;
                OnPropertyChanged("TaskStatus");
            }
        }
        public string SearchFoldersStatus
        {
            get { return __SearchFoldersStatus__; }
            set
            {
                if (value == __SearchFoldersStatus__) return;
                __SearchFoldersStatus__ = value;
                FolderStatus = TotalFolder + (string.IsNullOrEmpty(SearchFoldersStatus) ? "" : $"{SearchFoldersStatus}");
                OnPropertyChanged("SearchFoldersStatus");
            }
        }
        public string SearchFilesStatus
        {
            get { return __SearchFilesStatus__; }
            set
            {
                if (value == __SearchFilesStatus__) return;
                __SearchFilesStatus__ = value;
                FileStatus = TotalFile + (string.IsNullOrEmpty(SearchFilesStatus) ? "" : $"{SearchFilesStatus}");
                OnPropertyChanged("SearchFilesStatus");
            }
        }
        public string FileStatus
        {
            get { return __FileStatus__; }
            set
            {
                if (value == __FileStatus__) return;
                __FileStatus__ = value;
                OnPropertyChanged("FileStatus");
            }
        }
        public string FolderStatus
        {
            get { return __FolderStatus__; }
            set
            {
                if (value == __FolderStatus__) return;
                __FolderStatus__ = value;
                OnPropertyChanged("FolderStatus");
            }
        }
        public string CurrentFileAndSize
        {
            get { return __CurrentFileAndSize__; }
            set
            {
                if (value == __CurrentFileAndSize__) return;
                __CurrentFileAndSize__ = value;
                OnPropertyChanged("CurrentFileAndSize");
            }
        }
        #endregion
        volatile Tuple<long, long>
            fileProgress = new Tuple<long, long>(0, 0),
            folderProgress = new Tuple<long, long>(0, 0),
            sizeProgress = new Tuple<long, long>(0, 0),
            searchStatus = new Tuple<long, long>(0, 0);
        void UpdateMixedProgress()
        {
            const double createFileCost = 1024, createFolderCost = 1024;
            if (searchStatus.Item1 == 0 && searchStatus.Item2 == 0)
            {
                Progress =
                (sizeProgress.Item1 + fileProgress.Item1 * createFileCost + folderProgress.Item1 * createFolderCost) /
                (sizeProgress.Item2 + fileProgress.Item2 * createFileCost + folderProgress.Item2 * createFolderCost);
            }
        }
        enum ProgressType { File,Folder,Size,LocalSearch};
        void UpdateProgress(Tuple<long,long>p,ProgressType progressType)
        {
            switch(progressType)
            {
                case ProgressType.File:
                    {
                        fileProgress = p;
                        FileProgress = p.Item2 == 0 ? 1 : (double)p.Item1 / p.Item2;
                        CurrentFile = $"{Constants.Icons.File}{p.Item1}";
                        TotalFile = $"{Constants.Icons.File}{p.Item2}";
                    }
                    break;
                case ProgressType.Folder:
                    {
                        folderProgress = p;
                        FolderProgress = p.Item2 == 0 ? 1 : (double)p.Item1 / p.Item2;
                        CurrentFolder = $"{Constants.Icons.Folder}{p.Item1}";
                        TotalFolder = $"{Constants.Icons.Folder}{p.Item2}";
                    }
                    break;
                case ProgressType.Size:
                    {
                        sizeProgress = p;
                        SizeProgress = p.Item2 == 0 ? 1 : (double)p.Item1 / p.Item2;
                        CurrentSize = ByteCountToString(p.Item1, 3);
                        TotalSize = $"{ByteCountToString(p.Item2, 3)}";
                        OnSpeedDataAdded(p.Item1);
                    }
                    break;
                case ProgressType.LocalSearch:
                    {
                        searchStatus = p;
                        SearchFoldersStatus = p.Item1 == 0 ? "" : $"{Constants.Icons.Hourglass}{p.Item1}";
                        SearchFilesStatus = p.Item2 == 0 ? "" : $"{Constants.Icons.Hourglass}{p.Item2}";
                    }
                    break;
                default:
                    MyLogger.LogError($"Unexpected progressType: {progressType}");
                    break;
            }
            UpdateMixedProgress();
        }
        private void RegisterEvents(Local.Folder.Uploader up)
        {
            up.Completed += (success) => OnCompleted(success);
            up.MessageAppended += (msg) => OnMessageAppended(msg);
            up.Pausing += () => OnPausing();
            up.Started += () => OnStarted();
            up.RunningTaskCountChanged += (ts) =>
              {
                  if (ts == new Tuple<long, long>(0, 0)) TaskStatus = null;
                  else
                  {
                      TaskStatus = $"{(ts.Item1 == 0 ? Constants.Icons.Pause : Constants.Icons.Hourglass)}: {ts.Item1} / {ts.Item2}";
                  }
              };
            up.FileProgressChanged += (p) => UpdateProgress(p, ProgressType.File);
            up.FolderProgressChanged += (p) => UpdateProgress(p, ProgressType.Folder);
            up.SizeProgressChanged += (p) => UpdateProgress(p, ProgressType.Size);
            up.LocalSearchStatusChanged += (p) => UpdateProgress(p, ProgressType.LocalSearch);
        }
        public FolderUploadBarViewModel(Local.Folder.Uploader up):base()
        {
            Name = up.F.Name;
            PauseClicked = new Xamarin.Forms.Command(async () =>
              {
                  if (up.IsActive) up.Pause();
                  else await up.StartAsync();
              });
            RegisterEvents(up);
        }
    }
}
