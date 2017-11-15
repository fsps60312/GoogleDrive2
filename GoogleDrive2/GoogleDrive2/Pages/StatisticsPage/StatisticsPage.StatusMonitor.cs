using System;
using System.ComponentModel;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Globalization;

namespace GoogleDrive2.Pages.StatisticsPage
{
    partial class StatisticsPage
    {
        class StatusMonitor : System.ComponentModel.INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            void OnPropertyChanged(string propertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
            public int HttpRequestInstanceCount { get; private set; } = 0;
            public int HttpResponseInstanceCount { get; private set; } = 0;
            public int LocalFileInstanceCount { get; private set; } = 0;
            public int LocalFolderInstanceCount { get; private set; } = 0;
            public int FileUploaderInstanceCount { get; private set; } = 0;
            public int FolderUploaderInstanceCount { get; private set; } = 0;
            public int ApiOperatorInstanceCount { get; private set; } = 0;
            public int TreapNodeInstanceCount { get; private set; } = 0;
            public int FileUploadingCount { get; private set; } = 0;
            public int SmallFileUploadingCount { get; private set; } = 0;
            public int LargeFileUploadingCount { get; private set; } = 0;
            public int QueuedFileUploadingCount { get; private set; } = 0;
            public int SmallQueuedFileUploadingCount { get; private set; } = 0;
            public int LargeQueuedFileUploadingCount { get; private set; } = 0;
            public int WaitingForMetadataFileUploadingCount { get; private set; } = 0;
            public int FolderCreatingCount { get; private set; } = 0;
            public int QueuedFolderCreatingCount { get; private set; } = 0;
            public int WaitingForMetadataFolderCreatingCount { get; private set; } = 0;
            public long MemoryUsed { get; private set; } = 0;
            public StatusMonitor()
            {
                MyHttpRequest.InstanceCountChanged += (c) => { HttpRequestInstanceCount = c; OnPropertyChanged("HttpRequestInstanceCount"); };
                MyHttpResponse.InstanceCountChanged += (c) => { HttpResponseInstanceCount = c; OnPropertyChanged("HttpResponseInstanceCount"); };
                Local.File.InstanceCountChanged += (c) => { LocalFileInstanceCount = c; OnPropertyChanged("LocalFileInstanceCount"); };
                Local.File.Uploader.InstanceCountChanged += (c) => { FileUploaderInstanceCount = c; OnPropertyChanged("FileUploaderInstanceCount"); };
                Local.File.Uploader.RunningCountChanged += (c) => { FileUploadingCount = c; OnPropertyChanged("FileUploadingCount"); };
                Local.File.Uploader.RunningSmallFileUploadingCountChanged += (c) => { SmallFileUploadingCount = c; OnPropertyChanged("SmallFileUploadingCount"); };
                Local.File.Uploader.RunningLargeFileUploadingCountChanged += (c) => { LargeFileUploadingCount = c; OnPropertyChanged("LargeFileUploadingCount"); };
                Local.File.Uploader.QueuedCountChanged += (c) => { QueuedFileUploadingCount = c; OnPropertyChanged("QueuedFileUploadingCount"); };
                Local.File.Uploader.QueuedSmallFileCountChanged += (c) => { SmallQueuedFileUploadingCount = c; OnPropertyChanged("SmallQueuedFileUploadingCount"); };
                Local.File.Uploader.QueuedLargeFileCountChanged += (c) => { LargeQueuedFileUploadingCount = c; OnPropertyChanged("LargeQueuedFileUploadingCount"); };
                Local.File.Uploader.WaitingForMetadataCountChanged += (c) => { WaitingForMetadataFileUploadingCount = c; OnPropertyChanged("WaitingForMetadataFileUploadingCount"); };
                Local.Folder.InstanceCountChanged += (c) => { LocalFolderInstanceCount = c; OnPropertyChanged("LocalFolderInstanceCount"); };
                Local.Folder.Uploader.InstanceCountChanged += (c) => { FolderUploaderInstanceCount = c; OnPropertyChanged("FolderUploaderInstanceCount"); };
                Api.Files.FullCloudFileMetadata.FolderCreate.RunningCountChanged += (c) => { FolderCreatingCount = c; OnPropertyChanged("FolderCreatingCount"); };
                Api.Files.FullCloudFileMetadata.FolderCreate.QueuedCountChanged += (c) => { QueuedFolderCreatingCount = c; OnPropertyChanged("QueuedFolderCreatingCount"); };
                Api.Files.FullCloudFileMetadata.FolderCreate.WaitingForMetadataCountChanged += (c) => { WaitingForMetadataFolderCreatingCount = c; OnPropertyChanged("WaitingForMetadataFolderCreatingCount"); };
                Api.ApiOperator.InstanceCountChanged += (c) => { ApiOperatorInstanceCount = c; OnPropertyChanged("ApiOperatorInstanceCount"); };
                MyControls.BarsListPanel.TreapNodeStatistics.InstanceCountChanged += (c) => { TreapNodeInstanceCount = c; OnPropertyChanged("TreapNodeInstanceCount"); };
                new Action(async () =>
                {
                    while(true)
                    {
                        MemoryUsed = GC.GetTotalMemory(false);
                        OnPropertyChanged("MemoryUsed");
                        await Task.Delay(500);
                    }
                })();
            }
            public class InstanceCountValueConverter : IValueConverter
            {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    return ((int)value).ToString();
                }
                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    throw new NotImplementedException();
                }
            }
            public class MemoryValueConverter : IValueConverter
            {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    double memory = (long)value;
                    string unit = "B";
                    if (memory > 1024)
                    {
                        memory /= 1024;
                        unit = "KiB";
                        if (memory > 1024)
                        {
                            memory /= 1024;
                            unit = "MiB";
                            if (memory > 1024)
                            {
                                memory /= 1024;
                                unit = "GiB";
                                if (memory > 1024)
                                {
                                    memory /= 1024;
                                    unit = "TiB";
                                }
                            }
                        }
                    }
                    return $"{memory.ToString("F3")} {unit}";
                }
                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
