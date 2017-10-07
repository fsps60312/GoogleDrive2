using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDrive2
{
    class CloudFile
    {
        public string Id,Name;
        public bool IsFolder;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }
}
