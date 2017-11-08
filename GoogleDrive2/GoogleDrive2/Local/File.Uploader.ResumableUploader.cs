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
                private async Task<bool> DoUpload()
                {
                    return await StartResumableUploadAsync();
                }
                protected override async Task<bool> StartUploadAsync()
                {
                    if (CheckPause()) return false;
                    if (resumableUri == null)
                    {
                        this.Debug("Creating upload...");
                        if (!await CreateUpload())
                        {
                            this.LogError("Failed to create resumable upload");
                            return false;
                        }
                        else this.Debug("Upload created, uploading...");
                    }
                    if (CheckPause()) return false;
                    return await DoUpload();
                }
                public ResumableUploader(File file, Api.Files.FullCloudFileMetadata fileMetadata) : base(file, fileMetadata) { }
            }
        }
    }
}
