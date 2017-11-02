using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public abstract class UploaderPrototype : Api.AdvancedApiOperator
            {
                public event Libraries.Events.MyEventHandler<string> UploadCompleted;
                protected void OnUploadCompleted(string id)
                {
                    UploadCompleted?.Invoke(id);
                    OnCompleted(true);
                }
                public static async Task StartPrivateStaticAsync(UploaderPrototype up, bool startFromScratch)
                {
                    await up.StartPrivateAsync(startFromScratch);
                }
                public const long MinChunkSize = 262144;// + 1;
                protected File F;
                public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
                protected Api.Files.FullCloudFileMetadata FileMetadata;
                private long __BytesUploaded__ = 0;
                protected long BytesUploaded
                {
                    get { return __BytesUploaded__; }
                    set
                    {
                        if (__BytesUploaded__ == value) return;
                        __BytesUploaded__ = value;
                        ProgressChanged?.Invoke(Tuple.Create(value, TotalSize));
                    }
                }
                private long __TotalSize__ = 0;
                protected long TotalSize
                {
                    get { return __TotalSize__; }
                    set
                    {
                        if (__TotalSize__ == value) return;
                        __TotalSize__ = value;
                        ProgressChanged?.Invoke(Tuple.Create(BytesUploaded, value));
                    }
                }
                protected async Task AssignFileMetadata()
                {
                    TotalSize = (long)await F.GetSizeAsync();
                    FileMetadata.name = F.Name;
                    FileMetadata.createdTime = await F.GetTimeCreatedAsync();
                    FileMetadata.modifiedTime = await F.GetTimeModifiedAsync();
                }
                protected string ParseCloudId(string content)
                {
                    return JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(content).id;
                }
                protected abstract Task StartUploadAsync(bool startFromScratch);
                protected override async Task StartPrivateAsync(bool startFromScratch)
                {
                    try
                    {
                        F.CloseReadIfNot();
                        await AssignFileMetadata();
                        await StartUploadAsync(startFromScratch);
                    }
                    catch (Exception error) { this.LogError(error.ToString()); }
                    finally { F.CloseReadIfNot(); }
                }
                public UploaderPrototype(File file, Api.Files.FullCloudFileMetadata fileMetadata)
                {
                    F = file;
                    FileMetadata = fileMetadata;
                }
            }
        }
    }
}
