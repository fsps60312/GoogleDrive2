using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader:Api.AdvancedApiOperator
        {
            public static event Libraries.Events.MyEventHandler<Uploader> NewUploaderCreated;
            public event Libraries.Events.MyEventHandler<Tuple<long, long>> ProgressChanged;
            public event Libraries.Events.MyEventHandler<string> UploadCompleted;
            public File F { get; private set; }
            public Func<Task<Api.Files.FullCloudFileMetadata>> GetFileMetadata
            {
                get;private set;
            } = () => { return Task.FromResult(new Api.Files.FullCloudFileMetadata()); };
            public void SetFileMetadata(Func<Api.Files.FullCloudFileMetadata,Task<Api.Files.FullCloudFileMetadata>>func)
            {
                var preFunc = GetFileMetadata;
                GetFileMetadata = async () =>
                  {
                      return await func(await preFunc());
                  };
            }
            UploaderPrototype up = null;
            public async Task GetFileSizeFirstAsync()
            {
                ProgressChanged?.Invoke(new Tuple<long, long>(0, (long)await F.GetSizeAsync()));
            }
            int pauseRequest = 0;
            protected override async Task<bool>StartPrivateAsync()
            {
                System.Threading.Interlocked.Exchange(ref pauseRequest, 0);
                if (up == null)
                {
                    if (await F.GetSizeAsync() < UploaderPrototype.MinChunkSize) up = new MultipartUploader(F, await this.GetFileMetadata());
                    else up = new ResumableUploader(F, await this.GetFileMetadata());
                    up.Started += () => OnStarted();
                    up.ProgressChanged += (p) => ProgressChanged?.Invoke(p);
                    this.Pausing += () =>
                    {
                        OnDebugged("Pausing...");
                        up.Pause();
                    };
                    up.Completed += (success) => __OnCompleted(success);
                    up.Debugged += (msg) => OnDebugged(msg);
                    up.ErrorLogged += (msg) => OnErrorLogged(msg);// No need to add stacktrace again
                    up.UploadCompleted += (id) => UploadCompleted?.Invoke(id);
                    //up.MessageAppended is triggered by Debug & LogError
                }
                if (IsPausing) return false;
                return await up.StartAsync();
            }
            public Uploader(File file)
            {
                F = file;
                NewUploaderCreated?.Invoke(this);
            }
        }
    }
}
