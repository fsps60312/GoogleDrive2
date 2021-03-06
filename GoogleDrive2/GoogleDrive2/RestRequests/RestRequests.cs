﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace GoogleDrive2.RestRequests
{
    class RestRequestsPrototype : MyLoggerClass
    {
        public static async Task<string> LogHttpWebResponse(MyHttpResponse response, bool readStream)
        {
            if (response == null) return "(Null Response)";
            string ans = $"Http response: {response.StatusCode} ({(int)response.StatusCode})\r\n";
            StringBuilder sb = new StringBuilder();
            foreach (var p in response.Headers) sb.AppendLine($"{p.Key}:{JsonConvert.SerializeObject(p.Value)}");
            ans += sb.ToString() + "\r\n";
            if (readStream)
            {
                ans += (await response.GetResponseString() ?? "Error: Stream was not readable.") + "\r\n";
            }
            return ans;
        }
        public virtual async Task<MyHttpResponse> GetHttpResponseAsync(MyHttpRequest request)
        {
            return await request.GetResponseAsync();
        }
    }
    class RestRequestsLogger : RestRequestsPrototype
    {
        public override async Task<MyHttpResponse> GetHttpResponseAsync(MyHttpRequest request)
        {
            var ans = await base.GetHttpResponseAsync(request);
            if (!MyHttpRequest.IsSuccessfulStatus(ans?.StatusCode))
            {
                this.LogError(await LogHttpWebResponse(ans, true /*(int)(ans?.StatusCode ?? 0) / 100 != 2*/));
            }
            return ans;
        }
    }
    class RestRequestsRetrier : RestRequestsLogger
    {
        public override async Task<MyHttpResponse> GetHttpResponseAsync(MyHttpRequest request)
        {
            MyHttpResponse response = null;
            if (!await Libraries.MyExponentialBackOff.Do(new Func<Task<bool>>(async () =>
             {
                 response = await base.GetHttpResponseAsync(request);
                 switch (response?.StatusCode)
                 {
                     case null:
                     case HttpStatusCode.InternalServerError:
                     case HttpStatusCode.BadGateway:
                     case HttpStatusCode.ServiceUnavailable:
                     case HttpStatusCode.GatewayTimeout:
                     case HttpStatusCode.Forbidden://Rate Limit Exceeded
                         {
                             return false;
                         }
                     default: return true;
                 }
             }), new Func<int, Task>(async (timeToWait) =>
              {
                  var msg = $"Trying again {timeToWait} ms later...\r\nResponse: {await LogHttpWebResponse(response, true)}";
                  response?.Dispose();
                  response = null;
                  this.LogError(msg);
              })))
            {
                this.LogError($"Attempted to reconnect but still failed.\r\nResponse: {await LogHttpWebResponse(response, true)}");
            }
            return response;
        }
    }
    class RestRequestsLimiter : RestRequestsRetrier
    {
        const int MaxRequestPerSecond = 10;
        const int MaxConcurrentCount = 10;
        static DateTime front = DateTime.Now.AddSeconds(-1);
        static Queue<DateTime> history = new Queue<DateTime>();
        public static Libraries.MySemaphore semaphore { get; private set; } = new Libraries.MySemaphore(MaxConcurrentCount);
        public override async Task<MyHttpResponse> GetHttpResponseAsync(MyHttpRequest request)
        {
            await semaphore.WaitAsync();
            try
            {
                int timeToWait;
                lock (history)
                {
                    while (history.Count + 1 >= MaxRequestPerSecond) front = history.Dequeue();
                    var timeToStart = front.AddSeconds(1);
                    var timeNow = DateTime.Now;
                    if (timeNow >= timeToStart)
                    {
                        timeToWait = 0;
                        history.Enqueue(timeNow);
                    }
                    else
                    {
                        timeToWait = (int)((timeToStart - timeNow).TotalMilliseconds + 1);
                        //MyLogger.Debug($"(timeNow,timeToWait)=({timeNow},{timeToWait})");
                        history.Enqueue(timeToStart);
                    }
                }
                if (timeToWait > 0) await Task.Delay(timeToWait);
                return await base.GetHttpResponseAsync(request);
            }
            finally { semaphore.Release(); }
        }
    }

    partial class RestRequestsAuthorizer : RestRequestsLimiter
    {
        public bool AuthorizationRequired = true;
        //public RestRequestsAuthorizer(bool auth) { authorizationRequired = auth; }
        static int quotaUserNumber = 0;
        private async Task UpdateRequestAuthorization(MyHttpRequest request, bool refresh)
        {
            if (AuthorizationRequired)
            {
                //request.Uri += (request.Uri.IndexOf('?') == -1 ? "?" : "&") + $"quotaUser={WebUtility.UrlEncode($"fsps60312-GoogleDrive2-{quotaUserNumber++}")}";
                quotaUserNumber %= 5;
                request.Headers["Authorization"] = $"{await DriveAuthorizer.GetTokenTypeAsync()} {await DriveAuthorizer.GetAccessTokenAsync(refresh)}";
            }
        }
        public override async Task<MyHttpResponse> GetHttpResponseAsync(MyHttpRequest request)
        {
            await UpdateRequestAuthorization(request, false);
            var response = await base.GetHttpResponseAsync(request);
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                this.LogError($"Http response: {await RestRequests.RestRequester.LogHttpWebResponse(response, true)}");
                await Task.Delay(500);
                this.LogError("Refreshing access token...");
                //MyLogger.Assert(Array.IndexOf(request.Headers.AllKeys, "Authorization") != -1);
                await UpdateRequestAuthorization(request, true);
                response?.Dispose();
                response = await base.GetHttpResponseAsync(request);
                if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    this.LogError($"Failed to authenticate\r\nHttp response: {await RestRequests.RestRequester.LogHttpWebResponse(response, true)}");
                }
            }
            return response;
        }
    }
    partial class RestRequester : RestRequestsAuthorizer
    {
        public override async Task<MyHttpResponse> GetHttpResponseAsync(MyHttpRequest request)
        {
            try
            {
                return await base.GetHttpResponseAsync(request);
            }
            catch (System.Threading.Tasks.TaskCanceledException error)
            {
                MyLogger.LogError($"{error}");
                return null;
            }
        }
    }
}
