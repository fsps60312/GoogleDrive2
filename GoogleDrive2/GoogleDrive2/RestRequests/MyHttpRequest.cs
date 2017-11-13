using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using MyStream = System.IO.Stream;

namespace GoogleDrive2
{
    public partial class MyHttpRequest
    {
        //abstract class MyStream:System.IO.Stream
        //{
        //    //public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        //    //{
        //    //    return base.WriteAsync(buffer, offset, count, cancellationToken);
        //    //}
        //}
        public string Method;
        public string Uri;
        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();
        public event Libraries.Events.EmptyEventHandler Started, Writing, Requesting, Receiving, Received, Finished;
        public event Libraries.Events.MyEventHandler<HttpWebResponse> Responded;
        public event Libraries.Events.MyEventHandler<Tuple<long,long?>> ProgressChanged;
        public static event Libraries.Events.MyEventHandler<MyHttpRequest> NewRequestCreated;
        public string ContentType
        {
            get { return Headers["Content-Type"]; }
            set { Headers["Content-Type"] = value; }
        }
        public void CreateGetBodyMethod(Func<MyStream, Action<Tuple<long, long?>>, Task> method)
        {
            writeBodyTask = method;
        }
        public void CreateGetBodyMethod(byte[] bytes)
        {
            CreateGetBodyMethod(new Func<MyStream, Action<Tuple<long, long?>>, Task>(async (stream, progressChanged) =>
              {
                  progressChanged(new Tuple<long, long?>(0, bytes.Length));
                  await stream.WriteAsync(bytes, 0, bytes.Length);
                  progressChanged(new Tuple<long, long?>(bytes.Length, bytes.Length));
              }));
        }
        public override string ToString()
        {
            StringBuilder ans = new StringBuilder();
            ans.Append(Method); ans.Append(' '); ans.Append(Uri); ans.AppendLine();
            foreach (var p in Headers)
            {
                ans.Append(p.Key); ans.Append(":\t"); ans.Append(p.Value); ans.AppendLine();
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
                using (var stream = await httpRequest.GetRequestStreamAsync() as MyStream)
                {
                    await writeBodyTask(stream, new Action<Tuple<long, long?>>((p) => { ProgressChanged?.Invoke(p); }));
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
                response.ProgressChanged += (p) => { this.ProgressChanged?.Invoke(p); };
                return response;
            }
        }
        public static bool IsSuccessfulStatus(HttpStatusCode? code)
        {
            return code == HttpStatusCode.OK || (int?)code == 308;
        }
        Func<MyStream, Action<Tuple<long, long?>>, Task> writeBodyTask = null;
        DateTime start;
        byte[] EncodeToBytes(string s) { return Encoding.UTF8.GetBytes(s); }
        HttpWebRequest GetRequest()
        {
            var realRequest = HttpWebRequest.CreateHttp(Uri);
            realRequest.Method = Method;
            foreach (var header in Headers) realRequest.Headers[header.Key] = header.Value;
            realRequest.AllowReadStreamBuffering = false;
            realRequest.Proxy = null;
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
        static volatile int InstanceCount = 0;
        public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        static void AddInstanceCount(int value) { Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
        ~MyHttpRequest() { AddInstanceCount(-1); }
        public MyHttpRequest(string method, string uri)
        {
            AddInstanceCount(1);
            Method = method;
            Uri = uri;
            start = DateTime.Now;
            NewRequestCreated?.Invoke(this);
        }
    }
}