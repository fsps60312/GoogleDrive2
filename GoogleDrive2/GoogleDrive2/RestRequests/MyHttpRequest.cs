using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoogleDrive2
{
    public partial class MyHttpRequest
    {
        public string Method;
        public string Uri;
        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();
        public event Libraries.Events.EmptyEventHandler Started, Writing, Requesting, Receiving, Received, Finished;
        public event Libraries.Events.MyEventHandler<HttpWebResponse> Responded;
        public static event Libraries.Events.MyEventHandler<MyHttpRequest> NewRequestCreated;
        public string ContentType
        {
            get { return Headers["Content-Type"]; }
            set { Headers["Content-Type"] = value; }
        }
        public void CreateGetBodyMethod(byte[] bytes)
        {
            writeBodyTask = new Func<System.IO.Stream, Task>(async (stream) =>
              {
                  await stream.WriteAsync(bytes, 0, bytes.Length);
              });
        }
        public override string ToString()
        {
            StringBuilder ans = new StringBuilder();
            ans.Append(Method);ans.Append(' ');ans.Append(Uri);ans.AppendLine();
            foreach(var p in Headers)
            {
                ans.Append(p.Key);ans.Append(":\t");ans.Append(p.Value);ans.AppendLine();
            }
            ans.AppendLine();
            ans.AppendLine();
            if (response == null) ans.Append("(Not have respond yet)");
            else
            {
                ans.Append(response.ToString());
            }
            return ans.ToString();
        }
        MyHttpResponse response = null;
        Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
        public async Task<MyHttpResponse> GetResponseAsync()
        {
            Started?.Invoke();
            await semaphore.WaitAsync();
            var httpRequest = GetRequest();
            Writing?.Invoke();
            if (writeBodyTask != null)
            {
                using (var stream = await httpRequest.GetRequestStreamAsync())
                {
                    await writeBodyTask(stream);
                }
            }
            Requesting?.Invoke();
            var httpResponse = await GetResponse(httpRequest);
            Responded?.Invoke(httpResponse);
            if (httpResponse == null)
            {
                semaphore.Release();
                this.Finished?.Invoke();
                return null;
            }
            else
            {
                response = new MyHttpResponse(httpResponse);
                response.Receiving += delegate { this.Receiving?.Invoke(); };
                response.Received += delegate { this.Received?.Invoke(); };
                response.Disposed += delegate { semaphore.Release(); this.Finished?.Invoke(); };
                return response;
            }
        }
        Func<System.IO.Stream, Task> writeBodyTask = null;
        DateTime start;
        public MyHttpRequest(string method, string uri)
        {
            Method = method;
            Uri = uri;
            start = DateTime.Now;
            //this.Started += delegate { MyLogger.Debug($"Started {(DateTime.Now - start).TotalSeconds}"); };
            //this.Writing += delegate { MyLogger.Debug($"Writing {(DateTime.Now - start).TotalSeconds}"); };
            //this.Requesting += delegate { MyLogger.Debug($"Requesting {(DateTime.Now - start).TotalSeconds}"); };
            //this.Responded += delegate { MyLogger.Debug($"Responded {(DateTime.Now - start).TotalSeconds}"); };
            //this.Receiving += delegate { MyLogger.Debug($"Receiving {(DateTime.Now - start).TotalSeconds}"); };
            //this.Received += delegate { MyLogger.Debug($"Received {(DateTime.Now - start).TotalSeconds}"); };
            //this.Finished += delegate { MyLogger.Debug($"Finished {(DateTime.Now - start).TotalSeconds}"); };
            NewRequestCreated?.Invoke(this);
        }
        byte[] EncodeToBytes(string s) { return Encoding.UTF8.GetBytes(s); }
        HttpWebRequest GetRequest()
        {
            var realRequest = HttpWebRequest.CreateHttp(Uri);
            realRequest.Method = Method;
            foreach (var header in Headers) realRequest.Headers[header.Key] = header.Value;
            realRequest.AllowReadStreamBuffering = false;
            //realRequest.AllowWriteStreamBuffering = false;
            return realRequest;
        }
        async Task<HttpWebResponse> GetResponse(HttpWebRequest realRequest)
        {
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
            return response;
        }
    }
}
