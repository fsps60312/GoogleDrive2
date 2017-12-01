using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Api.Files
{
    public class MultipartUploadParameters : ParametersClass
    {
        public string uploadType = "multipart";
    }
    public class MultipartUpload : RequesterB<MultipartUploadParameters>
    {
        string DetermineSeperateString(byte[] metaBytes, byte[] fileBytes)
        {
            var ans = Guid.NewGuid().ToString();
            while (Libraries.StringAlgorithms.IndexOf(metaBytes, ans) != -1 || Libraries.StringAlgorithms.IndexOf(fileBytes, ans) != -1) ans = Guid.NewGuid().ToString();
            return ans;
        }
        string AssignBodyAndReturnBoundary(object metaData, byte[] fileContent)
        {
            var metaBytes = EncodeToBytes("Content-Type: application/json; charset=UTF-8\n\n" +
                JsonConvert.SerializeObject(metaData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DateFormatString = "yyyy-MM-ddTHH:mm:ssZ" }));
            var seperateString = DetermineSeperateString(metaBytes, fileContent);
            this.CreateBody((list) =>
            {
                list.AddRange(EncodeToBytes($"--{seperateString}\n"));
                {
                    list.AddRange(metaBytes);
                }
                list.AddRange(EncodeToBytes($"\n--{seperateString}\n\n"));
                {
                    list.AddRange(fileContent);
                }
                list.AddRange(EncodeToBytes($"\n--{seperateString}--"));
            });
            return seperateString;
        }
        public MultipartUpload(object metaData, byte[] fileContent) : base("POST", "https://www.googleapis.com/upload/drive/v3/files", true)
        {
            this.ContentType = $"multipart/related; charset=UTF-8; boundary={AssignBodyAndReturnBoundary(metaData, fileContent)}";
        }
    }
}
