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
        public abstract class SimpleApiOperator:ApiOperator
        {
            public abstract Task StartAsync();
        }
        public abstract class AdvancedApiOperator:ApiOperator
        {
            public abstract Task StartAsync(bool startFromScratch);
        }
        public class ApiOperator
        {
            public event Libraries.Events.MyEventHandler<string> UploadCompleted, ErrorOccurred;
            protected void OnUploadCompleted(string fileId) { UploadCompleted?.Invoke(fileId); }
            protected void OnErrorOccurred(string msg) { ErrorOccurred?.Invoke(msg); }
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
            public RequesterPrototype(string method, string uri, bool authorizationRequired)
            {
                Method = method;
                Uri = uri;
                requester.AuthorizationRequired = AuthorizationRequired = authorizationRequired;
                MyLogger.Assert(uri.IndexOf('?') == -1);
            }
            protected abstract Task<HttpWebRequest> GetHttpRequest();
            private RestRequests.RestRequester requester = new RestRequests.RestRequester();
            public async Task<HttpWebResponse> GetHttpResponseAsync()
            {
                var request = await this.GetHttpRequest();
                //await MyLogger.Alert(request.RequestUri.ToString());
                return await requester.GetHttpResponseAsync(request);
            }
            public async Task<string>GetResponseTextAsync(HttpWebResponse response)
            {
                try
                {
                    using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
                catch(Exception error)
                {
                    MyLogger.LogError($"Error when read response text:\r\n{error}");
                    return null;
                }
            }
        }
        public class RequesterRaw : RequesterPrototype
        {
            public RequesterRaw(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired) { }
            public Dictionary<string, string> Parameters = new Dictionary<string, string>();
            protected override Task<HttpWebRequest> GetHttpRequest()
            {
                StringBuilder uri = new StringBuilder(Uri);
                {
                    bool isFirst = true;
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
                var request = HttpWebRequest.CreateHttp(uri.ToString());
                request.Method = Method;
                return Task.FromResult(request);
            }
        }
        public class RequesterP<P>: RequesterPrototype where P : ParametersClass, new()
        {
            public RequesterP(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired) { }
            public P Parameters = new P();
            protected override async Task<HttpWebRequest> GetHttpRequest()
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
                var request = HttpWebRequest.CreateHttp(uri.ToString());
                request.Method = Method;
                return await Task.FromResult(request);
                //return new Task<HttpWebRequest>(() => request);
            }
        }
        class RequesterH<P> : RequesterP<P> where P : ParametersClass, new()
        {
            public RequesterH(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired) { }
            public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();
            protected override async Task<HttpWebRequest> GetHttpRequest()
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
        class RequesterB<P> : RequesterH<P> where P : ParametersClass, new()
        {
            protected string ContentType
            {
                get { return Headers["Content-Type"]; }
                set { Headers["Content-Type"] = value; }
            }
            public RequesterB(string method, string uri, bool authorizationRequired) : base(method, uri, authorizationRequired)
            {
            }
            public List<byte> Body { get; private set; } = new List<byte>();
            public byte[] EncodeToBytes(string s) { return Encoding.UTF8.GetBytes(s); }
            public void AppendBody(string s) { Body.AddRange(EncodeToBytes(s)); }
            protected override async Task<HttpWebRequest> GetHttpRequest()
            {
                var request = await base.GetHttpRequest();
                var bd = Body.ToArray();
                request.Headers["Content-Length"] = bd.Length.ToString();
                //await MyLogger.Alert(Encoding.UTF8.GetString(bd));
                using (System.IO.Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(bd, 0, bd.Length);
                }
                return request;
            }
        }
    }
}
