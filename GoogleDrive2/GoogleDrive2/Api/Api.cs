﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;

namespace GoogleDrive2
{
    namespace Api
    {
        public abstract class SimpleApiOperator:ApiOperator
        {
            public abstract Task StartAsync();
        }
        public abstract class AdvancedApiOperator:ApiOperator
        {
            public event Libraries.Events.EmptyEventHandler Started,Pausing,Paused;
            public event Libraries.Events.MyEventHandler<string> MessageAppended;
            protected void OnStarted() { Started?.Invoke(); }
            protected void OnPausing() { Pausing?.Invoke(); }
            protected void OnPaused() { Paused?.Invoke(); }
            protected bool CheckPause()
            {
                //this.Debug($"pauseRequest = {pauseRequest}");
                if (System.Threading.Interlocked.CompareExchange(ref pauseRequest, 2, 1) == 1)
                {
                    OnPaused();
                    return true;
                }
                else return false;
            }
            protected void OnDebugged(string msg) { Debug(msg, false); }
            protected void OnErrorLogged(string msg) { LogError(msg, false); }
            public bool IsActive { get { return pauseRequest == 0; } }
            protected bool IsPausing { get { return pauseRequest == 1; } }
            private int pauseRequest = 0;// 0: Normal, 1: Pausing, 2: Paused
            public void Pause()
            {
                if (IsCompleted) return;
                pauseRequest = 1;
                OnPausing();
                //if (IsCompleted)
                //{
                //    this.Debug("Pause: Operation has already completed");
                //    return;
                //}
            }
            protected abstract Task StartPrivateAsync();
            public async Task StartAsync()
            {
                if (IsCompleted) return;
                try
                {
                    Started?.Invoke();
                    int prePauseRequest = System.Threading.Interlocked.Exchange(ref pauseRequest, 0);
                    if (prePauseRequest == 1)
                    {
                        this.Debug("Pause Request Canceled");
                        return;// Cancel pauseRequest
                    }
                    else if(prePauseRequest==2)
                    {
                        this.Debug("Resumed");
                    }
                    else
                    {
                        this.Debug("Started");
                    }
                    if (IsCompleted)
                    {
                        this.LogError("StartAsync: Operation has already completed");
                        return;
                    }
                    await StartPrivateAsync();
                }
                catch(Exception error)
                {
                    this.LogError(error.ToString());
                }
            }
            public AdvancedApiOperator()
            {
                this.ErrorLogged += (error) => MessageAppended?.Invoke(Constants.Icons.Warning + error);
                this.Debugged += (msg) => MessageAppended?.Invoke(Constants.Icons.Info + msg);
                this.Completed += (success)=> { pauseRequest = success ? 0 : 2; };
            }
        }
        public class ApiOperator:MyLoggerClass
        {
            public bool IsCompleted = false;
            public event Libraries.Events.MyEventHandler<bool> Completed;
            protected void OnCompleted(bool success) { Completed?.Invoke(success); }
            protected ApiOperator() { Completed += (success) => { if (success) IsCompleted = true; }; }
        }
        public class ParametersClass
        {
            public string fields = null;// "nextPageToken,incompleteSearch,files(id,name,mimeType)";
            private static void AddParameter(Dictionary<string, string> ps, string s, object o, Type t, bool includeNull = false)
            {
                if (t == typeof(List<string>))
                {
                    if (includeNull || (o != null&&(o as List<string>).Count>0)) ps[s] = string.Join(",", o as List<string>);
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
                var response= await requester.GetHttpResponseAsync(request);
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
        public class RequesterP<P>: RequesterPrototype where P : ParametersClass, new()
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
            public async Task CreateBodyAsync(Func<List<byte>,Task>func)
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
                if(Body != null)request.CreateGetBodyMethod(Body);
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
