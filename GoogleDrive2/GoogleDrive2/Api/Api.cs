using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;

namespace GoogleDrive2
{
    namespace Api
    {
        //public abstract class SimpleApiOperator:ApiOperator
        //{
        //    protected abstract Task<bool> StartPrivateAsync();
        //    Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
        //    public async Task<bool> StartAsync()
        //    {
        //        await semaphore.WaitAsync();
        //        try
        //        {
        //            if (IsCompleted)
        //            {
        //                __OnCompleted(false);
        //                return false;
        //            }
        //            var completed = await StartPrivateAsync();
        //             __OnCompleted(completed);
        //            return completed;
        //        }
        //        finally { semaphore.Release(); }
        //    }
        //    protected SimpleApiOperator(){ }
        //}
        //public abstract class AdvancedApiOperator:ApiOperator
        //{
        //    public event Libraries.Events.EmptyEventHandler Started;
        //    public event Libraries.Events.MyEventHandler<object> Pausing;//Paused now replaced by Completed(false)
        //    public event Libraries.Events.MyEventHandler<string> MessageAppended;
        //    protected void OnStarted() { Started?.Invoke(); }
        //    protected void OnPausing() { Pausing?.Invoke(this); }
        //    protected void OnMessageAppended(string msg) { MessageAppended?.Invoke(msg); }
        //    protected bool CheckPause()
        //    {
        //        //this.Debug($"pauseRequest = {pauseRequest}");
        //        if (System.Threading.Interlocked.CompareExchange(ref pauseRequest, 2, 1) == 1) return true;
        //        else return false;
        //    }
        //    public bool IsActive { get { return pauseRequest == 0; } }
        //    protected bool IsPausing { get { return pauseRequest == 1; } }
        //    private volatile int pauseRequest = 0;// 0: Normal, 1: Pausing, 2: Paused
        //    public void Pause()
        //    {
        //        if (IsCompleted) return;
        //        System.Threading.Interlocked.Exchange(ref pauseRequest, 1);
        //        Pausing?.Invoke(this);
        //    }
        //    protected abstract Task<bool> StartPrivateAsync();
        //    Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
        //    public async Task<bool> StartAsync()
        //    {
        //        await semaphore.WaitAsync();
        //        try
        //        {
        //            Started?.Invoke();
        //            int prePauseRequest = System.Threading.Interlocked.Exchange(ref pauseRequest, 0);
        //            if (prePauseRequest == 1)
        //            {
        //                this.Debug($"{Constants.Icons.Info} Pause Request Canceled");
        //                return false;// Cancel pauseRequest
        //            }
        //            else if (prePauseRequest == 2)
        //            {
        //                this.Debug($"{Constants.Icons.Play} Resumed");
        //            }
        //            else
        //            {
        //                this.Debug($"{Constants.Icons.Play} Started");
        //            }
        //            if (IsCompleted)
        //            {
        //                this.LogError("StartAsync: Operation has already completed");
        //                __OnCompleted(false);
        //                return false;
        //            }
        //        }
        //        catch (Exception error)
        //        {
        //            this.LogError(error.ToString());
        //            __OnCompleted(false);
        //            return false;
        //        }
        //        finally { semaphore.Release(); }
        //        var completed = await StartPrivateAsync();
        //        __OnCompleted(completed);
        //        return completed;
        //    }
        //    protected AdvancedApiOperator()
        //    {
        //        this.Completed += (sender,success) => { pauseRequest = success ? 0 : 2; };
        //        this.Pausing += delegate { Debug($"{Constants.Icons.Pausing} Pausing..."); };
        //        this.ErrorLogged += (error) => OnMessageAppended($"{Constants.Icons.Warning} {error}");
        //        this.Debugged += (msg) => OnMessageAppended($"{msg}");
        //    }
        //}
        //public abstract class ApiOperator : MyLoggerClass
        //{
        //    public bool IsCompleted = false;
        //    public event Libraries.Events.MyEventHandler<object, bool> Completed;
        //    protected void __OnCompleted(bool success)
        //    {
        //        if (success) IsCompleted = true;
        //        Completed?.Invoke(this, success);
        //    }
        //    static volatile int InstanceCount = 0;
        //    public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        //    static void AddInstanceCount(int value) { System.Threading.Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
        //    ~ApiOperator() { AddInstanceCount(-1); }
        //    protected ApiOperator() { AddInstanceCount(1); }
        //}
        public class ParametersClass
        {
            public string fields = null;// "nextPageToken,incompleteSearch,files(id,name,mimeType)";
            private static void AddParameter(Dictionary<string, string> ps, string s, object o, Type t, bool includeNull = false)
            {
                if (t == typeof(List<string>))
                {
                    if (includeNull || (o != null && (o as List<string>).Count > 0)) ps[s] = string.Join(",", o as List<string>);
                }
                else if (t == typeof(bool?))
                {
                    if (includeNull || o != null)
                    {
                        ps[s] = ((bool?)o).HasValue ? (((bool?)o).Value ? "true" : "false") : null;
                    }
                }
                else if (t == typeof(int?))
                {
                    if (includeNull || o != null)
                    {
                        ps[s] = ((int?)o).HasValue ? ((int?)o).Value.ToString() : null;
                    }
                }
                else if (t == typeof(string))
                {
                    if (includeNull || !string.IsNullOrWhiteSpace(o as string))
                    {
                        //if(!includeNull)MyLogger.LogError($"Key: {s}, Value: {o as string}");
                        ps[s] = o as string;
                    }
                }
                else
                {
                    MyLogger.LogError($"Invalid type of parameter: {t}");
                }
            }
            //private static List<FieldInfo> GetFields(Type t)
            //{
            //    if (t == null) return new List<FieldInfo>();
            //    MyLogger.LogError(t.FullName);
            //    var ans = new List<FieldInfo>(t.GetFields());
            //    ans.AddRange(GetFields(t.GetTypeInfo().BaseType));
            //    return ans;
            //}
            public static void AddParameters<T>(T c, Dictionary<string, string> ps, bool includeNull = false) where T : ParametersClass, new()
            {
                foreach (var f in typeof(T).GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance))
                {
                    AddParameter(ps, f.Name, f.GetValue(c), f.FieldType, includeNull);
                }
            }
        }
        public abstract class RequesterPrototype
        {
            public string Method { get; private set; }
            public string Uri { get; private set; }
            public bool AuthorizationRequired { get; private set; }
            protected bool CheckUri = true;
            public RequesterPrototype(string method, string uri, bool authorizationRequired)
            {
                Method = method;
                Uri = uri;
                requester.AuthorizationRequired = AuthorizationRequired = authorizationRequired;
            }
            protected abstract Task<MyHttpRequest> GetHttpRequest();
            private RestRequests.RestRequester requester = new RestRequests.RestRequester();
            public async Task<MyHttpResponse> GetHttpResponseAsync()
            {
                var request = await this.GetHttpRequest();
                //await MyLogger.Alert(request.RequestUri.ToString());
                var response = await requester.GetHttpResponseAsync(request);
                return response;
            }
            public async Task<string> GetResponseTextAsync(MyHttpResponse response)
            {
                return await response.GetResponseString();
            }
        }
        public class RequesterRaw : RequesterPrototype
        {
            public RequesterRaw(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired) { }
            public Dictionary<string, string> Parameters = new Dictionary<string, string>();
            protected override Task<MyHttpRequest> GetHttpRequest()
            {
                StringBuilder uri = new StringBuilder(Uri);
                {
                    bool isFirst = (Uri.IndexOf('?') == -1);
                    if (CheckUri) MyLogger.Assert(isFirst);
                    foreach (var p in Parameters)
                    {
                        if (isFirst)
                        {
                            uri.Append('?');
                            isFirst = false;
                        }
                        else uri.Append('&');
                        uri.Append(WebUtility.UrlEncode(p.Key));
                        uri.Append('=');
                        uri.Append(WebUtility.UrlEncode(p.Value));
                    }
                }
                var request = new MyHttpRequest(Method, uri.ToString());
                return Task.FromResult(request);
            }
        }
        public class RequesterP<P> : RequesterPrototype where P : ParametersClass, new()
        {
            public RequesterP(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired)
            {
            }
            public P Parameters = new P();
            protected override async Task<MyHttpRequest> GetHttpRequest()
            {
                //AddParameter("fields", fields);
                StringBuilder uri = new StringBuilder(Uri);
                Dictionary<string, string> ps = new Dictionary<string, string>();
                ParametersClass.AddParameters(Parameters, ps);
                {
                    bool isFirst = true;
                    foreach (var p in ps)
                    {
                        if (isFirst)
                        {
                            uri.Append('?');
                            isFirst = false;
                        }
                        else uri.Append('&');
                        uri.Append(WebUtility.UrlEncode(p.Key));
                        uri.Append('=');
                        uri.Append(WebUtility.UrlEncode(p.Value));
                    }
                }
                //MyLogger.LogError(uri.ToString());
                var request = new MyHttpRequest(Method, uri.ToString());
                return await Task.FromResult(request);
                //return new Task<HttpWebRequest>(() => request);
            }
        }
        public class RequesterH<P> : RequesterP<P> where P : ParametersClass, new()
        {
            public RequesterH(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired) { }
            public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();
            protected override async Task<MyHttpRequest> GetHttpRequest()
            {
                var request = await base.GetHttpRequest();
                if (Headers != null)
                {
                    foreach (var p in Headers)
                    {
                        request.Headers[p.Key] = p.Value;
                    }
                }
                return request;
            }
        }
        public class RequesterB<P> : RequesterH<P> where P : ParametersClass, new()
        {
            protected string ContentType
            {
                get { return Headers["Content-Type"]; }
                set { Headers["Content-Type"] = value; }
            }
            public RequesterB(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired)
            {
            }
            public byte[] Body { get; protected set; } = null;
            public byte[] EncodeToBytes(string s) { return Encoding.UTF8.GetBytes(s); }
            Func<System.IO.Stream, Action<Tuple<long, long?>>, Task> createBodyMethod = null;
            long contentSize;
            public void ClearBody() { Body = null; }
            public void CreateBody(Action<List<byte>> func)
            {
                var list = new List<byte>();
                func(list);
                Body = list.ToArray();
            }
            public async Task CreateBodyAsync(Func<List<byte>, Task> func)
            {
                var list = new List<byte>();
                await func(list);
                Body = list.ToArray();
            }
            public void CreateBody(Func<System.IO.Stream, Action<Tuple<long, long?>>, Task> method, long size)
            {
                createBodyMethod = method;
                contentSize = size;
            }
            protected override async Task<MyHttpRequest> GetHttpRequest()
            {
                var request = await base.GetHttpRequest();
                request.Headers["Content-Length"] = Body == null ? contentSize.ToString() : Body.Length.ToString();
                //await MyLogger.Alert(Encoding.UTF8.GetString(bd));
                if (Body != null) request.CreateGetBodyMethod(Body);
                else
                {
                    MyLogger.Assert(createBodyMethod != null);
                    request.CreateGetBodyMethod(createBodyMethod);
                }
                return request;
            }
        }
    }
}
