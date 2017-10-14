using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
//using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;

namespace GoogleDrive2
{
    public partial class MyHttpRequest
    {
        public string Method;
        public string Uri;
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
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
        public async Task<MyHttpResponse> GetResponseAsync()
        {
            Started?.Invoke();
            var handler
                //= new ProgressMessageHandler(new HttpClientHandler { MaxRequestContentBufferSize = 1024 });
            = new HttpClientHandler { MaxRequestContentBufferSize = 1024 };
            //handler.HttpSendProgress += (sender, args) =>
            //{
            //    MyLogger.Debug($"HttpSend: {args.UserState}, {args.ProgressPercentage}, {args.BytesTransferred}, {args.TotalBytes}");
            //};
            //handler.HttpReceiveProgress += (sender, args) =>
            //{
            //    MyLogger.Debug($"HttpReceive: {args.UserState}, {args.ProgressPercentage}, {args.BytesTransferred}, {args.TotalBytes}");
            //};
            //handler.MaxAutomaticRedirections = 0;
            handler.AllowAutoRedirect = false;
            HttpClient client = new HttpClient(handler, true);
            var msg = new HttpRequestMessage(new HttpMethod(Method), Uri);
            if (Bytes != null)
            {
                MyLogger.Debug("Has body");
                msg.Content = new PushStreamContent(new Func<System.IO.Stream, HttpContent, System.Net.TransportContext, Task>(async (stream, content, transportContext) =>
                 {
                     int now = 0;
                     var bytes = Bytes.ToArray();
                     while (now < Bytes.Count)
                     {
                         var cnt = Math.Min(10240, Bytes.Count - now);
                         await stream.WriteAsync(bytes, now, cnt);
                         await stream.FlushAsync();
                         MyLogger.Debug($"{(double)now / Bytes.Count}");
                         now += cnt;
                     }
                     stream.Dispose();
                 }));
                msg.Headers.TransferEncodingChunked = true;
                foreach (var p in Headers)
                {
                    switch(p.Key)
                    {
                        case "Content-Length":msg.Content.Headers.ContentLength = long.Parse(p.Value);break;
                        case "Content-Type":
                            {
                                System.Net.Http.Headers.MediaTypeHeaderValue type = null;
                                foreach(var s in p.Value.Split(';').Select(s => s.Trim(' ')).SkipWhile(s => string.IsNullOrWhiteSpace(s)))
                                {
                                    if (type == null) type = new System.Net.Http.Headers.MediaTypeHeaderValue(s);
                                    else
                                    {
                                        var idx = s.IndexOf('=');
                                        MyLogger.Assert(idx != -1);
                                        var a = s.Remove(idx);
                                        var b = s.Substring(idx + 1);
                                        if (a == "charset") type.CharSet = b;
                                        else type.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue(a, b));
                                    }
                                }
                                MyLogger.Assert(type != null);
                                msg.Content.Headers.ContentType = type;
                                break;
                            }
                        default:
                            msg.Headers.Add(p.Key, p.Value);
                            break;
                    }
                }
            }
            else
            {
                MyLogger.Debug("No body");
                if (msg.Content != null) msg.Content = null;
                foreach (var p in Headers) msg.Headers.Add(p.Key, p.Value);
            }
            MyLogger.Debug($"{Method} {Uri}");
            foreach (var p in Headers) MyLogger.Debug($"{p}");
            client.MaxResponseContentBufferSize = 1 << 8;
            Requested?.Invoke();
            var response = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
            Responded?.Invoke();
            var ans= new MyHttpResponse(response);
            ans.Receiving += delegate { MyLogger.Debug($"Receiving {(DateTime.Now - start).TotalSeconds}"); };
            ans.Received += delegate { MyLogger.Debug($"Received {(DateTime.Now - start).TotalSeconds}"); };
            return ans;
        }
        DateTime start;
        public MyHttpRequest(string method, string uri)
        {
            Method = method;
            Uri = uri;
            start = DateTime.Now;
            this.Started += delegate { MyLogger.Debug($"Started {(DateTime.Now - start).TotalSeconds}"); };
            this.Requested += delegate { MyLogger.Debug($"Requested {(DateTime.Now - start).TotalSeconds}"); };
            this.Responded += delegate { MyLogger.Debug($"Responded {(DateTime.Now - start).TotalSeconds}"); };
        }
        event Libraries.Events.EmptyEventHandler Started, Requested, Responded;
        byte[] EncodeToBytes(string s) { return Encoding.UTF8.GetBytes(s); }
    }
}
