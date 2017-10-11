using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace GoogleDrive2
{
    public class MyHttpRequest
    {
        public string Method;
        public string Uri;
        public WebHeaderCollection Headers = new WebHeaderCollection();
        public string ContentType
        {
            get { return Headers["Content-Type"]; }
            set { Headers["Content-Type"] = value; }
        }
        List<byte> Bytes = null;
        public void WriteBytes(IEnumerable<byte> bytes)
        {
            if (Bytes == null) Bytes = new List<byte>();
            Bytes.AddRange(bytes);
        }
        public MyHttpRequest(string method, string uri)
        {
            Method = method;
            Uri = uri;
        }
        byte[] EncodeToBytes(string s) { return Encoding.UTF8.GetBytes(s); }
        public async Task<MyHttpResponse> GetResponseAsync()
        {
            var realRequest = HttpWebRequest.CreateHttp(Uri);
            realRequest.Method = Method;
            realRequest.Headers = Headers;
            if (Bytes != null)
            {
                using (var stream = await realRequest.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(Bytes.ToArray(), 0, Bytes.Count);
                }
            }
            HttpWebResponse response;
            try
            {
                response = await realRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException error)
            {
                response = error.Response as HttpWebResponse;
            }
            finally
            {
                realRequest.Abort();
            }
            if (response == null) return null;
            else
            {
                var ans = new MyHttpResponse(response);
                await ans.ReadStreamAsync();
                return ans;
            }
        }
    }
}
