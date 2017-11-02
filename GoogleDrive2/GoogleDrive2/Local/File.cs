using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    abstract class AFile
    {
        public abstract File.Uploader GetUploader();
        public abstract bool IsImageFile { get; }
        public abstract bool IsVideoFile { get; }
        public abstract bool IsMusicFile { get; }
        public abstract Task<ulong> GetSizeAsync();
        public abstract Task<DateTime> GetTimeCreatedAsync();
        public abstract Task<DateTime> GetTimeModifiedAsync();
        public abstract string Name { get; }
        public abstract string MimeType { get; }
        protected abstract Task OpenReadIfNotAsync();
        protected abstract Task OpenWriteIfNotAsync();
        public abstract Task WriteBytesAsync(byte[] array);
        public abstract Task SeekReadAsync(long position);
        public abstract Task<int> ReadAsync(byte[] array, int offset, int count);
        public abstract Task<byte[]> ReadBytesAsync(int count);
        public abstract void CloseReadIfNot();
        public abstract void CloseWriteIfNot();
    }
    partial class File:AFile
    {
        public override Uploader GetUploader() { return new Uploader(this); }
        public override bool IsImageFile
        {
            get { return MimeType.StartsWith("image"); }
        }
        public override bool IsVideoFile
        {
            get { return MimeType.StartsWith("video"); }
        }
        public override bool IsMusicFile
        {
            get { return MimeType.StartsWith("audio"); }
        }
        public static async Task<File> OpenSingleFileAsync()
        {
            return await OpenSingleFilePrivateAsync();
        }
        public static async Task<List<File>>OpenMultipleFilesAsync()
        {
            return await OpenMultipleFilesPrivateAsync();
        }
        static volatile int InstanceCount = 0;
        public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        static void AddInstanceCount(int value)
        {
            System.Threading.Interlocked.Add(ref InstanceCount, value);
            InstanceCountChanged?.Invoke(InstanceCount);
        }
        private File()
        {
            AddInstanceCount(1);
        }
        ~File()
        {
            CloseReadIfNot();
            AddInstanceCount(-1);
        }
    }
}
