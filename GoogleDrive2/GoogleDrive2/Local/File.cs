using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public static async Task<File> OpenSingleFileAsync()
        {
            return await OpenSingleFilePrivateAsync();
        }
    }
}
