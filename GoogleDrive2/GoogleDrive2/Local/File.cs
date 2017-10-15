using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public Uploader GetUploader() { return new Uploader(this); }
        public bool IsImageFile
        {
            get { return MimeType.StartsWith("image"); }
        }
        public bool IsVideoFile
        {
            get { return MimeType.StartsWith("video"); }
        }
        public bool IsMusicFile
        {
            get { return MimeType.StartsWith("audio"); }
        }
        public static async Task<File> OpenSingleFileAsync()
        {
            return await OpenSingleFilePrivateAsync();
        }
        static volatile int InstanceCount = 0;
        public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        Libraries.MySemaphore semaphoreInstance = new Libraries.MySemaphore(1);
        async void AddInstanceCount(int value)
        {
            await semaphoreInstance.WaitAsync();
            InstanceCountChanged?.Invoke(InstanceCount += value);
            semaphoreInstance.Release();
        }
        public File()
        {
            AddInstanceCount(1);
        }
        ~File()
        {
            CloseFileIfNot();
            AddInstanceCount(-1);
        }
    }
}
