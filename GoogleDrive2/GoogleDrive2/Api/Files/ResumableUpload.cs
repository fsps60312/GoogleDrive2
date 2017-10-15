using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Api.Files
{
    public class ResumableUploadParameters : ParametersClass
    {
    }
    public class ResumableUpload : RequesterB<ResumableUploadParameters>
    {
        public ResumableUpload(string uri,long fileSize,long startByte,long endByte,byte[]fileContent) : this(uri, fileSize, startByte, endByte)
        {
            MyLogger.Assert(fileContent.Length == endByte - startByte + 1);
            this.Body = fileContent;
        }
        //public ResumableUpload(string uri, long fileSize, long startByte, long endByte, Func<System.IO.Stream, Action<Tuple<long, long?>>, Task> createBodyMethod,long contentSize) : this(uri,fileSize,startByte,endByte)
        //{
        //    this.CreateBody(createBodyMethod,contentSize);
        //}
        private ResumableUpload(string uri, long fileSize, long startByte, long endByte): base("PUT", uri, true)
        {
            //MyLogger.Debug($"{startByte} {endByte} {fileSize}");
            this.CheckUri = false;
            MyLogger.Assert(0 <= startByte && startByte <= endByte && endByte < fileSize);
            this.Headers["Content-Range"] = $"bytes {startByte}-{endByte}/{fileSize}";
        }
        public ResumableUpload(string uri,long? fileSize) : base("PUT", uri, true)
        {
            this.CheckUri = false;
            this.Headers["Content-Range"] = $"bytes */{(fileSize.HasValue ? fileSize.Value.ToString() : "*")}";
            this.Body = new byte[0];
        }
    }
    public class ResumableCreateParameters : ParametersClass
    {
        public string uploadType = "resumable";
    }
    public class ResumableCreate:RequesterB<ResumableCreateParameters>
    {
        void AssignBody(object metaData)
        {
            this.Body = EncodeToBytes(JsonConvert.SerializeObject(metaData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DateFormatString = "yyyy-MM-ddTHH:mm:ssZ" }));
        }
        public ResumableCreate(object metaData,long? fileSize,string mimeType):base("POST", "https://www.googleapis.com/upload/drive/v3/files", true)
        {
            this.ContentType = "application/json; charset=UTF-8";
            if (mimeType != null) this.Headers["X-Upload-Content-Type"] = mimeType;
            if (fileSize != null) this.Headers["X-Upload-Content-Length"] = fileSize.Value.ToString();
            AssignBody(metaData);
        }
    }
}
