using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Linq;

namespace GoogleDrive2.Local
{
    partial class Folder:AFolder
    {
        public override string Name { get { return O.Name; } }
        static Libraries.MySemaphore searchSemaphore = new Libraries.MySemaphore(1);
        public override async Task<List<File>> GetFilesAsync()
        {
            await searchSemaphore.WaitAsync();
            try { return (await O.GetFilesAsync()).Select((f) => new File(f)).ToList(); }
            finally { searchSemaphore.Release(); }
        }
        public override async Task<List<Folder>> GetFoldersAsync()
        {
            await searchSemaphore.WaitAsync();
            try { return (await O.GetFoldersAsync()).Select((f) => new Folder(f)).ToList(); }
            finally { searchSemaphore.Release(); }
        }
        StorageFolder O;
        private static async Task<Folder> OpenSingleFolderPrivateAsync()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker()
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Clear();
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null) return new Folder { O = folder };
            else return null;
        }
        public Folder(StorageFolder o):this() { O = o; }
    }
}
