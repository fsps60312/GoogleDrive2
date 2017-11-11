using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.NetworkStatusPage.FolderUploadPage
{
    partial class FolderUploadBarViewModel : NetworkStatusWithSpeedBarViewModel
    {
        #region Properties
        long? __CurrentFile__ = null;
        long? __TotalFile__ = null;
        long? __CurrentFolder__ = null;
        long? __TotalFolder__ = null;
        long? __CurrentSize__ = null;
        long? __TotalSize__ = null;
        long? __SearchFoldersStatus__ = null;
        long? __SearchFilesStatus__ = null;
        string __TaskStatus__ = null;
        System.Windows.Input.ICommand __FoldClicked__;
        bool __IsFolded__ = false;
        public bool IsFolded
        {
            get { return __IsFolded__; }
            set
            {
                if (value == __IsFolded__) return;
                __IsFolded__ = value;
                OnPropertyChanged("IsFolded");
            }
        }
        public System.Windows.Input.ICommand FoldClicked
        {
            get { return __FoldClicked__; }
            set
            {
                if (value == __FoldClicked__) return;
                __FoldClicked__ = value;
                OnPropertyChanged("FoldClicked");
            }
        }
        public long? CurrentFile
        {
            get { return __CurrentFile__; }
            set
            {
                if (value == __CurrentFile__) return;
                __CurrentFile__ = value;
                OnPropertyChanged("CurrentFile");
            }
        }
        public long? TotalFile
        {
            get { return __TotalFile__; }
            set
            {
                if (value == __TotalFile__) return;
                __TotalFile__ = value;
                OnPropertyChanged("TotalFile");
            }
        }
        public long? CurrentFolder
        {
            get { return __CurrentFolder__; }
            set
            {
                if (value == __CurrentFolder__) return;
                __CurrentFolder__ = value;
                OnPropertyChanged("CurrentFolder");
            }
        }
        public long? TotalFolder
        {
            get { return __TotalFolder__; }
            set
            {
                if (value == __TotalFolder__) return;
                __TotalFolder__ = value;
                OnPropertyChanged("TotalFolder");
            }
        }
        public long? CurrentSize
        {
            get { return __CurrentSize__; }
            set
            {
                if (value == __CurrentSize__) return;
                __CurrentSize__ = value;
                OnPropertyChanged("CurrentSize");
            }
        }
        public long? TotalSize
        {
            get { return __TotalSize__; }
            set
            {
                if (value == __TotalSize__) return;
                __TotalSize__ = value;
                OnPropertyChanged("TotalSize");
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
        public long? SearchFoldersStatus
        {
            get { return __SearchFoldersStatus__; }
            set
            {
                if (value == __SearchFoldersStatus__) return;
                __SearchFoldersStatus__ = value;
                OnPropertyChanged("SearchFoldersStatus");
            }
        }
        public long? SearchFilesStatus
        {
            get { return __SearchFilesStatus__; }
            set
            {
                if (value == __SearchFilesStatus__) return;
                __SearchFilesStatus__ = value;
                OnPropertyChanged("SearchFilesStatus");
            }
        }
        #endregion

        #region Extended Properties
        public override double Progress
        {
            get
            {
                RegisterBinding(new List<string> { "FileProgress", "FolderProgress", "SizeProgress" }, "Progress");
                const double createFileCost = 1024, createFolderCost = 1024;
                if (searchStatus.Item1 == 0 && searchStatus.Item2 == 0)
                {
                    var ans = (CurrentSize + CurrentFile * createFileCost + CurrentFolder * createFolderCost) / (TotalSize + TotalFile * createFileCost + TotalFolder * createFolderCost);
                    return ans.HasValue ? ans.Value : 0;
                }
                else return 0;
            }
        }
        public string FileStatus
        {
            get
            {
                RegisterBinding(new List<string> { "TotalFile", "SearchFilesStatus" }, "FileStatus");
                return $"{GetFileText(TotalFile)}{GetSearchText(SearchFilesStatus)}";
            }
        }
        public string FolderStatus
        {
            get
            {
                RegisterBinding(new List<string> { "TotalFolder", "SearchFoldersStatus" }, "FolderStatus");
                return $"{GetFolderText(TotalFolder)}{GetSearchText(SearchFoldersStatus)}";
            }
        }
        public double SizeProgress
        {
            get
            {
                RegisterBinding(new List<string> { "CurrentSize", "TotalSize" }, "SizeProgress");
                var ans = TotalSize == 0 ? 1 : (double?)CurrentSize / TotalSize;
                return ans.HasValue ? ans.Value : 0;
            }
        }
        public double FileProgress
        {
            get
            {
                RegisterBinding(new List<string> { "CurrentFile", "TotalFile" }, "FileProgress");
                var ans = TotalFile == 0 ? 1 : (double?)CurrentFile / TotalFile;
                return ans.HasValue ? ans.Value : 0;
            }
        }
        public double FolderProgress
        {
            get
            {
                RegisterBinding(new List<string> { "CurrentFolder", "TotalFolder" }, "FolderProgress");
                if (!CurrentFolder.HasValue || !TotalFolder.HasValue) return 0;
                var ans= TotalFolder == 0 ? 1 : (double?)CurrentFolder / TotalFolder;
                return ans.HasValue ? ans.Value : 0;
            }
        }
        #endregion

        static string GetSearchText(long? v) { return v.HasValue && v.Value != 0 ? $"{Constants.Icons.Magnifier}{v}" : null; }
        static string GetSizeText(long? v) { return v.HasValue ? ByteCountToString(v.Value, 3) : null; ; }
        static string GetFolderText(long? v) { return v.HasValue ? $"{Constants.Icons.Folder}{v}" : null; }
        static string GetFileText(long? v) { return v.HasValue ? $"{Constants.Icons.File}{v}" : null; }
        public class SizeTextValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return GetSizeText((long?)value);
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        public class FolderTextValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return GetFolderText((long?)value);
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        public class FileTextValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return GetFileText((long?)value);
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public FolderUploadBarViewModel(Local.Folder.Uploader up) : base()
        {
            this.PropertyChanged += (o, p) =>
              {
                  if (!PropertyChangedEventChain.ContainsKey(p.PropertyName)) return;
                  foreach (var np in PropertyChangedEventChain[p.PropertyName]) OnPropertyChanged(np);
              };
            Name = up.F.Name;
            this.Indent = up.GetIndent();
            PauseClicked = new Xamarin.Forms.Command(async () =>
              {
                  if (up.IsActive) up.Pause();
                  else await up.StartAsync();
              });
            FoldClicked = new Xamarin.Forms.Command(async () =>
              {
                  throw new NotImplementedException();//TODO
              });
            RegisterEvents(up);
        }
    }
    partial class FolderUploadBarViewModel
    {
        Dictionary<string, HashSet<string>> PropertyChangedEventChain = new Dictionary<string, HashSet<string>>();
        void RegisterBinding(List<string> sources, string target)
        {
            foreach (var s in sources) RegisterBinding(s, target);
        }
        void RegisterBinding(string source, string target)
        {
            if (!PropertyChangedEventChain.ContainsKey(source)) PropertyChangedEventChain.Add(source, new HashSet<string>());
            PropertyChangedEventChain[source].Add(target);
        }
        volatile Tuple<long, long>
            searchStatus = new Tuple<long, long>(0, 0);
        enum ProgressType { File, Folder, Size, LocalSearch };
        void UpdateProgress(Tuple<long, long> p, ProgressType progressType)
        {
            switch (progressType)
            {
                case ProgressType.File:
                    {
                        CurrentFile = p.Item1;
                        TotalFile = p.Item2;
                    }
                    break;
                case ProgressType.Folder:
                    {
                        CurrentFolder = p.Item1;
                        TotalFolder = p.Item2;
                    }
                    break;
                case ProgressType.Size:
                    {
                        CurrentSize = p.Item1;
                        TotalSize = p.Item2;
                        OnSpeedDataAdded(p.Item1);
                    }
                    break;
                case ProgressType.LocalSearch:
                    {
                        searchStatus = p;
                        SearchFoldersStatus = p.Item1;
                        SearchFilesStatus = p.Item2;
                    }
                    break;
                default:
                    MyLogger.LogError($"Unexpected progressType: {progressType}");
                    break;
            }
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
                    TaskStatus = $"{(ts.Item1 == 0 ? (ts.Item2 == 0 ? Constants.Icons.Completed : Constants.Icons.Pause) : Constants.Icons.Hourglass)}: {ts.Item1} / {ts.Item2}";
                }
            };
            up.FileProgressChanged += (p) => UpdateProgress(p, ProgressType.File);
            up.FolderProgressChanged += (p) => UpdateProgress(p, ProgressType.Folder);
            up.SizeProgressChanged += (p) => UpdateProgress(p, ProgressType.Size);
            up.LocalSearchStatusChanged += (p) => UpdateProgress(p, ProgressType.LocalSearch);
        }
    }
}