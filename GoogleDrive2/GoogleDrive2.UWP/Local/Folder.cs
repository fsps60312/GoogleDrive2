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
        public override async Task<List<File>> GetFilesAsync()
        {
            return (await O.GetFilesAsync()).Select((f) => new File(f)).ToList();
        }
        public override async Task<List<Folder>>GetFoldersAsync()
        {
            return (await O.GetFoldersAsync()).Select((f) => new Folder(f)).ToList();
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
