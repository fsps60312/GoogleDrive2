﻿using System;
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
        public Dictionary<string,string> Headers
        {
            get
            {
                Dictionary<string, string> ans = new Dictionary<string, string>();
                foreach (var k in O.Headers.AllKeys) ans[k] = O.Headers[k];
                return ans;
            }
        }
        string toString = null;
        public override string ToString()
        {
            if (toString != null) return toString;
            try
            {
                StringBuilder ans = new StringBuilder();
                ans.Append($"{StatusCode}({(int)StatusCode})"); ans.Append(' '); ans.Append(O.StatusDescription); ans.AppendLine();
                ans.Append(O.Method); ans.Append(' '); ans.Append(O.ResponseUri); ans.AppendLine();
                foreach (var p in Headers)
                {
                    ans.Append(p.Key); ans.Append(":\t"); ans.Append(p.Value); ans.AppendLine();
                }
                return ans.ToString();
            }
            catch(Exception error)
            {
                return error.ToString();
            }
        }
        public event Libraries.Events.EmptyEventHandler Receiving, Received,Disposed;
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
            toString = ToString();
            O?.Dispose();
            Bytes = null;
            Disposed?.Invoke();
        }
        HttpWebResponse O;
        bool proccessed = false, dataLosed = false;
        static string DecodeToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
