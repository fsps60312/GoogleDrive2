using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace GoogleDrive2.RestRequests
{
    class RestRequestsPrototype: MyLoggerClass
    {
        public static async Task<string> LogHttpWebResponse(HttpWebResponse response, bool readStream)
        {
            if (response == null) return "(Null Response)";
            string ans = $"Http response: {response.StatusCode} ({(int)response.StatusCode})\r\n";
            StringBuilder sb = new StringBuilder();
            foreach (var key in response.Headers.AllKeys) sb.AppendLine($"{key}:{JsonConvert.SerializeObject(response.Headers[key])}");
            ans += sb.ToString() + "\r\n";
            if (readStream)
            {
                try
                {
                    var reader = new System.IO.StreamReader(response.GetResponseStream());
                    ans += await reader.ReadToEndAsync() + "\r\n";
                    reader.Dispose();
                }
                catch (ArgumentException error)
                {
                    if (error.Message != "Stream was not readable.") throw error;
                    ans+= $"Error: {error.Message}\r\n";
                }
            }
            return ans;
        }
        public virtual async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            try
            {
                return (await request.GetResponseAsync()) as HttpWebResponse;
            }
            catch (WebException error)
            {
                return error.Response as HttpWebResponse;
            }
            finally
            {
                request.Abort();
            }
        }
    }
    class RestRequestsLogger: RestRequestsPrototype
    {
        public override async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            var ans = await base.GetHttpResponseAsync(request);
            if (ans?.StatusCode != HttpStatusCode.Accepted) this.Log(await LogHttpWebResponse(ans, (int)(ans?.StatusCode ?? 0) / 100 != 2));
            return ans;
        }
    }
    class RestRequestsRetrier : RestRequestsLogger
    {
        public override async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            HttpWebResponse response=null;
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
                  this.Log($"Trying again {timeToWait} ms later...\r\nResponse: {await LogHttpWebResponse(response, true)}");
              })))
            {
                this.LogError($"Attempted to reconnect but still failed.\r\nResponse: {await LogHttpWebResponse(response, false)}");
            }
            return response;
        }
    }
    class RestRequestsLimiter: RestRequestsRetrier
    {
        private Libraries.MySemaphore semaphore = new Libraries.MySemaphore(50);
        public override async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            await semaphore.WaitAsync();
            try
            {
                return await base.GetHttpResponseAsync(request);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    partial class RestRequestsAuthorizer:RestRequestsLimiter
    {
        public bool AuthorizationRequired = true;
        //public RestRequestsAuthorizer(bool auth) { authorizationRequired = auth; }
        private async Task UpdateRequestAuthorization(HttpWebRequest request,bool refresh)
        {
            if (AuthorizationRequired)
            {
                request.Headers["Authorization"] = $"{await DriveAuthorizer.GetTokenTypeAsync()} {await DriveAuthorizer.GetAccessTokenAsync(refresh)}";
            }
        }
        public override async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            await UpdateRequestAuthorization(request,false);
            var response = await base.GetHttpResponseAsync(request);
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                this.Log($"Http response: {await RestRequests.RestRequester.LogHttpWebResponse(response, true)}");
                await Task.Delay(500);
                this.Log("Refreshing access token...");
                //MyLogger.Assert(Array.IndexOf(request.Headers.AllKeys, "Authorization") != -1);
                await UpdateRequestAuthorization(request, true);
                response = await base.GetHttpResponseAsync(request);
                if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    this.LogError($"Failed to authenticate\r\nHttp response: {await RestRequests.RestRequester.LogHttpWebResponse(response, false)}");
                }
            }
            return response;
        }
    }
    partial class RestRequester:RestRequestsAuthorizer
    {
        public override async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            try
            {
                return await base.GetHttpResponseAsync(request);
            }
            catch(System.Threading.Tasks.TaskCanceledException error)
            {
                MyLogger.LogError($"{error}");
                return null;
            }
        }
    }
}
