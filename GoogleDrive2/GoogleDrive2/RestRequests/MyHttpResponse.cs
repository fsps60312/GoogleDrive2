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
        public event Libraries.Events.EmptyEventHandler Receiving, Received;
        public MyHttpResponse(HttpWebResponse o)
        {
            MyLogger.Assert(o != null);
            O = o;
        }
        public System.IO.Stream GetResponseStream()
        {
            dataLosed = true;
            return O.GetResponseStream();
        }
        public async Task<string> GetResponseString()
        {
            MyLogger.Assert(!dataLosed);
            if (!proccessed) await ReadStreamAsync();
            if (Bytes == null) return null;
            return DecodeToString(Bytes.ToArray());
        }
        public async Task ReadStreamAsync()
        {
            MyLogger.Assert(!dataLosed);
            MyLogger.Assert(!proccessed);
            proccessed = true;
            Receiving?.Invoke();
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
            Received?.Invoke();
        }
        public void Dispose()
        {
            O?.Dispose();
            Bytes = null;
        }
        HttpWebResponse O;
        bool proccessed = false, dataLosed = false;
        static string DecodeToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
