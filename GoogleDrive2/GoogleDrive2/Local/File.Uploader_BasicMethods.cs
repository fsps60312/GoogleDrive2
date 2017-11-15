using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public abstract partial class Uploader
        {
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
            protected string ParseCloudId(string content)
            {
                try
                {
                    return JsonConvert.DeserializeObject<Api.Files.FullCloudFileMetadata>(content).id;
                }
                catch (Exception error)
                {
                    MyLogger.LogError($"Error when ParseCloudId:\r\n{error}");
                    return null;
                }
            }
        }
    }
}
