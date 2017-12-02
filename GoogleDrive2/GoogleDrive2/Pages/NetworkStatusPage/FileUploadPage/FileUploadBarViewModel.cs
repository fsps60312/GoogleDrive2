using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage.FileUploadPage
{
    partial class FileUploadBarViewModel : NetworkStatusWithSpeedBarViewModel
    {
        #region Properties
        string __Uploaded__ = null;
        string __Total__ = null;
        public string Total
        {
            get { return __Total__; }
            set
            {
                if (value == __Total__) return;
                __Total__ = value;
                OnPropertyChanged("Total");
            }
        }
        public string Uploaded
        {
            get { return __Uploaded__; }
            set
            {
                if (value == __Uploaded__) return;
                __Uploaded__ = value;
                OnPropertyChanged("Uploaded");
            }
        }
        #endregion
        private void RegisterEvents(Local.File.Uploader up)
        {
            up.Unstarted += (sender) => OnCompleted(up.IsCompleted);
            up.MessageAppended += (msg) => OnMessageAppended(msg);
            up.Pausing += delegate { OnPausing(); };
            up.Started += delegate { OnStarted(); };
            up.ProgressChanged += (p) =>
            {
                Progress = p.Item2 == 0 ? 1 : (double)p.Item1 / p.Item2;
                OnSpeedDataAdded(p.Item1);
                Uploaded = ByteCountToString(p.Item1, 3);
                Total = ByteCountToString(p.Item2, 3);
            };
        }
        public FileUploadBarViewModel(Local.File.Uploader up) : base()
        {
            Name = up.F.Name;
            PauseClicked = new Xamarin.Forms.Command(async () =>
              {
                  //this.OnMessageAppended($"Click {up.IsActive}");
                  if (up.IsRunning)await up.PauseBackgroundAsync();
                  else await up.StartBackgroundAsync();
              });
            RegisterEvents(up);
        }
    }
}