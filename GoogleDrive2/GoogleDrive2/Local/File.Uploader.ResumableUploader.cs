using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public partial class ResumableUploader : Uploader
            {
                public const long FileBufferSize = 1 << 20;
                public const double DesiredProgressUpdateInterval = 0.5;
                string resumableUri = null;
                private async Task<bool> CreateUpload()
                {
                    return await CreateResumableUploadAsync();
                }
                private async Task<bool> DoUpload()
                {
                    return await StartResumableUploadAsync();
                }
                protected override async Task<bool> StartUploadAsync()
                {
                    if (CheckPause()) return false;
                    if (resumableUri == null)
                    {
                        this.Debug($"{Constants.Icons.Hourglass} Creating upload...");
                        if (!await CreateUpload())
                        {
                            this.LogError($"{Constants.Icons.Info} Resumable upload create paused or failed");
                            return false;
                        }
                        else this.Debug($"{Constants.Icons.Hourglass} Upload created, uploading...");
                    }
                    if (CheckPause()) return false;
                    return await DoUpload();
                }
                public ResumableUploader(File file) : base(file) { }
            }
        }
    }
}
