using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GoogleDrive2
{
    public class MyHttpResponse:IDisposable
    {
        public HttpStatusCode StatusCode { get { return O.StatusCode; } }
        public List<byte> Bytes { get; private set; } = null;
        public HttpResponseHeaders Headers { get { return O.Headers; } }
        public event Libraries.Events.EmptyEventHandler Receiving, Received;
        public MyHttpResponse(HttpResponseMessage o)
        {
            MyLogger.Assert(o != null);
            O = o;
        }
        public async Task<System.IO.Stream> GetResponseStream()
        {
            dataLosed = true;
            return await O.Content.ReadAsStreamAsync();
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
            using (var stream =await O.Content.ReadAsStreamAsync())
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
        HttpResponseMessage O;
        bool proccessed = false, dataLosed = false;
        static string DecodeToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
