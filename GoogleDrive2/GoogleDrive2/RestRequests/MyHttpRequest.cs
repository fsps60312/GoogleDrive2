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
        public async Task<MyHttpResponse> GetResponseAsync()
        {
            await PerformTasks1();
            if (Bytes != null)
            {
                using (var stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(Bytes.ToArray(), 0, Bytes.Count);
                }
            }
            var response= await PerformTasks2();
            response.Receiving+= delegate { MyLogger.Debug($"Receiving {(DateTime.Now - start).TotalSeconds}"); };
            response.Received+= delegate { MyLogger.Debug($"Received {(DateTime.Now - start).TotalSeconds}"); };
            return response;
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
        HttpWebRequest GetRequest()
        {
            var realRequest = HttpWebRequest.CreateHttp(Uri);
            realRequest.Method = Method;
            realRequest.Headers = Headers;
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
        HttpWebResponse response;
        HttpWebRequest request;
        Libraries.MySemaphore semaphore = null;
        async void SendRequest(HttpWebRequest realRequest)
        {
            semaphore = new Libraries.MySemaphore(0);
            response = await GetResponse(realRequest);
            semaphore.Release();
        }
        Task PerformTasks1()
        {
            Started?.Invoke();
            request = GetRequest();
            Requested?.Invoke();
            SendRequest(request);
            return Task.CompletedTask;
        }
        async Task<MyHttpResponse> PerformTasks2()
        {
            MyLogger.Assert(semaphore != null);
            await semaphore.WaitAsync();
            MyLogger.Assert(response != null);
            Responded?.Invoke();
            if (response == null) return null;
            else
            {
                var ans = new MyHttpResponse(response);
                return ans;
            }
        }
    }
}
