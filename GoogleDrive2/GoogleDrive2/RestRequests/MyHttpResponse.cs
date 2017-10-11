using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace GoogleDrive2
{
    public class MyHttpResponse:IDisposable
    {
        public HttpStatusCode StatusCode { get { return O.StatusCode; } }
        public List<byte> Bytes { get; private set; } = null;
        public WebHeaderCollection Headers { get { return O.Headers; } }
        HttpWebResponse O;
        bool proccessed=false;
        public MyHttpResponse(HttpWebResponse o)
        {
            MyLogger.Assert(o != null);
            O = o;
        }
        static string DecodeToString(byte[]bytes) { return Encoding.UTF8.GetString(bytes); }
        public string GetResponseString()
        {
            if (Bytes == null) return null;
            return DecodeToString(Bytes.ToArray());
        }
        public async Task ReadStreamAsync()
        {
            MyLogger.Assert(!proccessed);
            proccessed = true;
            using (var stream = O.GetResponseStream())
            {
                if(stream!=null)
                {
                    Bytes = new List<byte>();
                    var buffer = new byte[1<<10];
                    for (int len = 0; (len = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0;)
                    {
                        for (int i = 0; i < len; i++) Bytes.Add(buffer[i]);
                    }
                }
            }
        }
        public void Dispose()
        {
            O?.Dispose();
            Bytes = null;
        }
    }
}
