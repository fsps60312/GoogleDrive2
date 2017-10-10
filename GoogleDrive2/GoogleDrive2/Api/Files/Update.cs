using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoogleDrive2.Api.Files
{
    class UpdateParameters: ParametersClass
    {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
        public string uploadType;
        public System.Collections.Generic.List<string> addParents;
        public bool? keepRevisionForever;//default: false
        public string ocrLanguage;
        public System.Collections.Generic.List<string> removeParents;
        public bool? supportsTeamDrives/*default: false*/, useContentAsIndexableText/*default: false*/;
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
    }
    class UpdateMetadata: RequesterB<UpdateParameters>
    {
        public UpdateMetadata(string fileId,object metadata):base("PATCH", $"https://www.googleapis.com/drive/v3/files/{fileId}",true)
        {
            this.ContentType = "application/json; charset=UTF-8";
            this.Body.Clear();
            this.Body.AddRange(EncodeToBytes(JsonConvert.SerializeObject(metadata, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DateFormatString = "yyyy-MM-ddTHH:mm:ssZ" })));
        }
    }
}
