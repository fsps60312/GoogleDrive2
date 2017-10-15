using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace GoogleDrive2.Local
{
    partial class File
    {
        StorageFile O;
        public async Task<Api.Files.FullCloudFileMetadata.VideoMediaMetadataClass>GetVideoMediaMetadataAsync()
        {
            var p = await O.Properties.GetVideoPropertiesAsync();
            var ans = new Api.Files.FullCloudFileMetadata.VideoMediaMetadataClass
            {
                width = (int)p.Width,
                height = (int)p.Height,
                durationMillis = (long)p.Duration.TotalMilliseconds
            };
            return ans;
        }
        public async Task<Api.Files.FullCloudFileMetadata.ImageMediaMetadataClass>GetImageMediaMetadataAsync()
        {
            var p = await O.Properties.GetImagePropertiesAsync();
            var ans = new Api.Files.FullCloudFileMetadata.ImageMediaMetadataClass
            {
                width = (int)p.Width,
                height = (int)p.Height,
                location = new Api.Files.FullCloudFileMetadata.ImageMediaMetadataClass.LocationClass
                {
                    latitude = p.Latitude,
                    longitude = p.Longitude,
                    altitude = p.Height
                },
                time = p.DateTaken.UtcDateTime.ToString("yyyy:MM:dd HH:mm:ss"),
                cameraMake = p.CameraManufacturer,
                cameraModel = p.CameraModel
            };
            switch (p.Orientation)
            {
                case Windows.Storage.FileProperties.PhotoOrientation.Normal: ans.rotation = 0; break;
                case Windows.Storage.FileProperties.PhotoOrientation.Rotate90: ans.rotation = 90; break;
                case Windows.Storage.FileProperties.PhotoOrientation.Rotate180: ans.rotation = 180; break;
                case Windows.Storage.FileProperties.PhotoOrientation.Rotate270: ans.rotation = 270; break;
                case Windows.Storage.FileProperties.PhotoOrientation.Unspecified: ans.rotation = null; break;
                default: MyLogger.LogError($"PhotoOrientation not supported: {p.Orientation}"); break;
            }
            return ans;
        }
        public async Task<ulong> GetSizeAsync()
        {
            return (await O.GetBasicPropertiesAsync()).Size;
        }
        public Task<DateTime> GetTimeCreatedAsync()
        {
            return Task.FromResult(O.DateCreated.UtcDateTime);
        }
        public async Task<DateTime> GetTimeModifiedAsync()
        {
            return (await O.GetBasicPropertiesAsync()).DateModified.UtcDateTime;
        }
        public string Name { get { return O.Name; } }
        public string MimeType { get { return O.ContentType; } }
        Stream readStream = null, writeStream = null;
        private async Task OpenReadIfNotAsync()
        {
            CloseWriteIfNot();
            if (readStream == null) readStream = await O.OpenStreamForReadAsync();
        }
        private async Task OpenWriteIfNotAsync()
        {
            CloseReadIfNot();
            if (writeStream == null) writeStream = await O.OpenStreamForWriteAsync();
        }
        public async Task WriteBytesAsync(byte[]array)
        {
            await OpenWriteIfNotAsync();
            await writeStream.WriteAsync(array, 0, array.Length);
        }
        public async Task SeekReadAsync(long position)
        {
            await OpenReadIfNotAsync();
            readStream.Seek(position, SeekOrigin.Begin);
        }
        public async Task<int>ReadAsync(byte[]array,int offset,int count)
        {
            await OpenReadIfNotAsync();
            return await readStream.ReadAsync(array, offset, count);
        }
        public async Task<byte[]> ReadBytesAsync(int count)
        {
            await OpenReadIfNotAsync();
            var ans = new byte[count];
            int i = 0;
            while (i < count)
            {
                i += await readStream.ReadAsync(ans, i, count - i);
            }
            return ans;
        }
        public void CloseReadIfNot()
        {
            if (readStream != null)
            {
                readStream.Dispose();
                readStream = null;
            }
        }
        public void CloseWriteIfNot()
        {
            if (writeStream != null)
            {
                writeStream.Dispose();
                writeStream = null;
            }
        }
        private static async Task<File> OpenSingleFilePrivateAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker()
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Clear();
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            //await MyLogger.Alert(Libraries.MySerializer.SerializeProperties(file));
            //var p = await file.GetBasicPropertiesAsync();
            //await MyLogger.Alert(Libraries.MySerializer.SerializeProperties(p));
            //var q = await file.Properties.GetImagePropertiesAsync();
            //await MyLogger.Alert(Libraries.MySerializer.SerializeProperties(q));
            //var q = await file.Properties.GetDocumentPropertiesAsync();
            //await MyLogger.Alert(Libraries.MySerializer.SerializeProperties(q));
            if (file != null) return new File { O = file };
            else return null;
        }
    }
}