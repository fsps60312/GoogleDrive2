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
            protected override Task StartPrivateAsync()
            {
                //should not be called
                throw new NotImplementedException("Should not be called");
            }
            public new async Task StartAsync()
            {
                if (up == null)
                {
                    if (await F.GetSizeAsync() < UploaderPrototype.MinChunkSize) up = new MultipartUploader(F, await this.GetFileMetadata());
                    else up = new ResumableUploader(F, await this.GetFileMetadata());
                    up.Started += () => OnStarted();
                    up.ProgressChanged += (p) => ProgressChanged?.Invoke(p);
                    up.Paused += () =>
                    {
                        OnDebugged("Paused");
                        OnPaused();
                    };
                    up.Pausing += () =>
                    {
                        OnDebugged("Pausing...");
                        OnPausing();
                    };
                    up.Completed += (success) => OnCompleted(success);
                    up.Debugged += (msg) => OnDebugged(msg);
                    up.ErrorLogged += (msg) => OnErrorLogged(msg);// No need to add stacktrace again
                    up.UploadCompleted += (id) => UploadCompleted?.Invoke(id);
                    //up.MessageAppended is triggered by Debug & LogError
                }
                await up.StartAsync();
            }
            public new void Pause() { up.Pause(); }
            public new bool IsActive { get { return up.IsActive; } }
            public Uploader(File file)
            {
                F = file;
                NewUploaderCreated?.Invoke(this);
            }
        }
    }
}
