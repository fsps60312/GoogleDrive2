using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Globalization;

namespace GoogleDrive2.Pages.StatisticsPage
{
    class StatisticsPage:MyContentPage
    {
        class WatcherBinding : System.ComponentModel.INotifyPropertyChanged
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
            public long MemoryUsed { get; private set; } = 0;
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
            public WatcherBinding()
            {
                MyHttpRequest.InstanceCountChanged += (c) => { HttpRequestInstanceCount = c; OnPropertyChanged("HttpRequestInstanceCount"); };
                MyHttpResponse.InstanceCountChanged += (c) => { HttpResponseInstanceCount = c; OnPropertyChanged("HttpResponseInstanceCount"); };
                Local.File.InstanceCountChanged += (c) => { LocalFileInstanceCount = c; OnPropertyChanged("LocalFileInstanceCount"); };
                Local.Folder.InstanceCountChanged += (c) => { LocalFolderInstanceCount = c; OnPropertyChanged("LocalFolderInstanceCount"); };
                Local.File.Uploader.InstanceCountChanged += (c) => { FileUploaderInstanceCount = c; OnPropertyChanged("FileUploaderInstanceCount"); };
                Local.Folder.Uploader.InstanceCountChanged += (c) => { FolderUploaderInstanceCount = c; OnPropertyChanged("FolderUploaderInstanceCount"); };
                Api.ApiOperator.InstanceCountChanged += (c) => { ApiOperatorInstanceCount = c; OnPropertyChanged("ApiOperatorInstanceCount"); };
                MyControls.BarsListPanel.TreapNodeStatistics.InstanceCountChanged += (c) => { TreapNodeInstanceCount = c; OnPropertyChanged("TreapNodeInstanceCount"); };
                new Action(async () =>
                {
                    while(true)
                    {
                        MemoryUsed = System.GC.GetTotalMemory(true);
                        OnPropertyChanged("MemoryUsed");
                        await Task.Delay(500);
                    }
                })();
            }
        }
        MyGrid GDmain;
        MyScrollView SVmain;
        MyButton BTNclearMemory;
        private void RegisterEvents()
        {
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
            this.BindingContext = new WatcherBinding();
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
                    {
                        AddWatcher("Memory Used", "MemoryUsed", new WatcherBinding.MemoryValueConverter());
                        AddWatcher("Http Request Instance", "HttpRequestInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("Http Response Instance", "HttpResponseInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("File Instance", "LocalFileInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("File Uploader Instance", "FileUploaderInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("Folder Instance", "LocalFolderInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("Folder Uploader Instance", "FolderUploaderInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("Api Operator Instance", "ApiOperatorInstanceCount", new WatcherBinding.InstanceCountValueConverter());
                        AddWatcher("TreapNode Instance", "TreapNodeInstanceCount", new WatcherBinding.InstanceCountValueConverter());
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
