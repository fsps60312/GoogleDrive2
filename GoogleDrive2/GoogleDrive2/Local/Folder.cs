using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace GoogleDrive2.Local
{
    abstract class AFolder
    {
        public abstract Folder.Uploader GetUploader();
        public abstract Task<List<File>> GetFilesAsync();
        public abstract Task<List<Folder>> GetFoldersAsync();
        public abstract Task<DateTime> GetTimeCreatedAsync();
        public abstract Task<DateTime> GetTimeModifiedAsync();
        public abstract string Name { get; }
    }

    partial class Folder:AFolder
    {
        public override Uploader GetUploader() { return new Uploader(this); }
        public override Task<DateTime> GetTimeCreatedAsync()
        {
            return Task.FromResult(O.DateCreated.UtcDateTime);
        }
        public override async Task<DateTime> GetTimeModifiedAsync()
        {
            return (await O.GetBasicPropertiesAsync()).DateModified.UtcDateTime;
        }
        public static async Task<Folder> OpenSingleFolderAsync() { return await OpenSingleFolderPrivateAsync(); }
        static volatile int InstanceCount = 0;
        public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        static void AddInstanceCount(int value)
        {
            System.Threading.Interlocked.Add(ref InstanceCount, value);
            InstanceCountChanged?.Invoke(InstanceCount);
        }
        private Folder()
        {
            AddInstanceCount(1);
        }
        ~Folder()
        {
            AddInstanceCount(-1);
        }
    }
}
