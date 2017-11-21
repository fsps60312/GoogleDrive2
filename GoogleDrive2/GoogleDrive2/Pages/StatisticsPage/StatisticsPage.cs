using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.StatisticsPage
{
    partial class StatisticsPage:MyContentPage
    {
        MyGrid GDmain;
        MyScrollView SVmain;
        MyButton BTNclearMemory,BTNdeleteAuthorationSaveFile;
        private void RegisterEvents()
        {
            BTNdeleteAuthorationSaveFile.Clicked += async delegate
            {
                BTNdeleteAuthorationSaveFile.IsEnabled = false;
                await RestRequests.RestRequestsAuthorizer.DriveAuthorizer.DeleteSaveFileAsync();
                await MyLogger.Alert("Cached Authorization Saved File deleted, you might need to log in your Google Drive again.");
                BTNdeleteAuthorationSaveFile.IsEnabled = true;
            };
            BTNclearMemory.Clicked += delegate
              {
                  BTNclearMemory.IsEnabled = false;
                  var startTime = DateTime.Now;
                  GC.Collect(GC.MaxGeneration, System.GCCollectionMode.Forced, true);
                  BTNclearMemory.Text = $"{Constants.Icons.Clear} Clear Memory ({(int)((DateTime.Now-startTime).TotalMilliseconds)} ms)";
                  BTNclearMemory.IsEnabled = true;
              };
        }
        private void AddWatcher(string name,string binding,IValueConverter converter)
        {
            var row = GDmain.RowDefinitions.Count;
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50, GridUnitType.Absolute) });
            {
                var l = new MyLabel { Text = name };
                l.Margin = new Thickness(0, 0, 50, 0);
                GDmain.Children.Add(l, 0, row);
            }
            {
                var l = new MyLabel();
                l.SetBinding(MyLabel.TextProperty, binding, BindingMode.Default, converter);
                GDmain.Children.Add(l, 1, row);
            }
        }
        private void InitializeViews()
        {
            this.BindingContext = new StatusMonitor();
            {
                SVmain = new MyScrollView();
                {
                    GDmain = new MyGrid();
                    GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50, GridUnitType.Absolute) });
                    {
                        BTNclearMemory = new MyButton { Text = $"{Constants.Icons.Clear} Clear Memory" };
                        GDmain.Children.Add(BTNclearMemory, 1, 0);
                    }
                    GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50, GridUnitType.Absolute) });
                    {
                        BTNdeleteAuthorationSaveFile = new MyButton { Text = $"{Constants.Icons.TrashCan} Delete Cached Authorization Data" };
                        GDmain.Children.Add(BTNdeleteAuthorationSaveFile, 2, 0);
                    }
                    {
                        AddWatcher("Memory Used", "MemoryUsed", new StatusMonitor.MemoryValueConverter());
                        AddWatcher("Http Request Instance", "HttpRequestInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Http Response Instance", "HttpResponseInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Instance", "LocalFileInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Folder Instance", "LocalFolderInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploader Instance", "FileUploaderInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Waiting for Metadata", "WaitingForMetadataFileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Queued", "QueuedFileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Queued (Small Files)", "SmallQueuedFileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Queued (Large Files)", "LargeQueuedFileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Running", "FileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Running (Small Files)", "SmallFileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("File Uploading: Running (Large Files)", "LargeFileUploadingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Folder Uploader Instance", "FolderUploaderInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Folder Creating: Wairing for Metadata", "WaitingForMetadataFolderCreatingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Folder Creating: Queued", "QueuedFolderCreatingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Folder Creating: Running", "FolderCreatingCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("Api Operator Instance", "ApiOperatorInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                        AddWatcher("TreapNode Instance", "TreapNodeInstanceCount", new StatusMonitor.InstanceCountValueConverter());
                    }
                    SVmain.Content = GDmain;
                }
                this.Content = SVmain;
            }
        }
        public StatisticsPage()
        {
            this.Title = "Statistics";
            InitializeViews();
            RegisterEvents();
        }
    }
}
