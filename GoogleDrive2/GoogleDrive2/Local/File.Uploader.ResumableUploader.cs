using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public partial class Uploader
        {
            public partial class ResumableUploader : UploaderPrototype
            {
                public const long FileBufferSize = 1 << 20;
                public const double DesiredProgressUpdateInterval = 0.5;
                string resumableUri = null;
                private async Task<bool> CreateUpload()
                {
                    return await CreateResumableUploadAsync();
                }
                private async Task DoUpload()
                {
                    await StartResumableUploadAsync();
                }
                protected override async Task StartUploadAsync(bool startFromScratch)
                {
                    if (CheckPause()) return;
                    if (startFromScratch)
                    {
                        if (!await CreateUpload())
                        {
                            this.LogError("Failed to create resumable upload");
                            return;
                        }
                    }
                    if (CheckPause()) return;
                    if (resumableUri == null)
                    {
                        this.Debug("Seems upload not created yet, starting from scratch...");
                        await StartUploadAsync(true);
                    }
                    else
                    {
                        await DoUpload();
                    }
                }
                public ResumableUploader(File file, Api.Files.FullCloudFileMetadata fileMetadata) : base(file, fileMetadata) { }
            }
        }
    }
}
