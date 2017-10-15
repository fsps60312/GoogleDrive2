using System.ComponentModel;
using System.Threading.Tasks;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    partial class NetworkStatusPage
    {
        class HttpStatistics : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            string __RequestCount__ = "Active Requests: 0 (Perhaps)";
            string __ResponseCount__ = "Active Responses: 0 (Perhaps)";
            string __FileCount__ = "Opened Files: 0 (Perhaps)";
            string __MemoryUsed__ = "Memory Used: Unknown";
            public string RequestCount
            {
                get { return __RequestCount__; }
                set
                {
                    if (__RequestCount__ == value) return;
                    __RequestCount__ = value;
                    OnPropertyChanged("RequestCount");
                }
            }
            public string ResponseCount
            {
                get { return __ResponseCount__; }
                set
                {
                    if (__ResponseCount__ == value) return;
                    __ResponseCount__ = value;
                    OnPropertyChanged("ResponseCount");
                }
            }
            public string FileCount
            {
                get { return __FileCount__; }
                set
                {
                    if (__FileCount__ == value) return;
                    __FileCount__ = value;
                    OnPropertyChanged("FileCount");
                }
            }
            public string MemoryUsed
            {
                get { return __MemoryUsed__; }
                set
                {
                    if (__MemoryUsed__ == value) return;
                    __MemoryUsed__ = value;
                    OnPropertyChanged("MemoryUsed");
                }
            }
            public HttpStatistics()
            {
                MyHttpRequest.InstanceCountChanged += (c) => { RequestCount= $"Active Requests: {c}"; };
                MyHttpResponse.InstanceCountChanged += (c) => { ResponseCount= $"Active Responses: {c}"; };
                Local.File.InstanceCountChanged += (c) => { FileCount = $"Opened Files: {c}"; };
                var monitorMemoryTask = Task.Run(async () =>
                  {
                      while (true)
                      {
                          double memory = System.GC.GetTotalMemory(false);
                          string unit = "B";
                          if (memory > 1024)
                          {
                              memory /= 1024;
                              unit = "KB";
                              if (memory > 1024)
                              {
                                  memory /= 1024;
                                  unit = "MB";
                                  if (memory > 1024)
                                  {
                                      memory /= 1024;
                                      unit = "GB";
                                      if (memory > 1024)
                                      {
                                          memory /= 1024;
                                          unit = "TB";
                                      }
                                  }
                              }
                          }
                          MemoryUsed = $"Memory Used: {memory.ToString("F3").PadLeft(8)} {unit}";
                          await Task.Delay(500);
                      }
                  });
            }
        }
    }
}
