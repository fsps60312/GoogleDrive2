using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace GoogleDrive2.RestRequests
{
    partial class RestRequestsAuthorizer
    {
        public partial class DriveAuthorizer
        {
            static readonly string auth_uri = "https://accounts.google.com/o/oauth2/auth";
            static readonly string token_uri = "https://accounts.google.com/o/oauth2/token";
            static readonly string client_id = "767856013993-4jsq7q0iujolnd9bomvd36ff4md16rv7.apps.googleusercontent.com";
            static readonly string client_secret = "fqHbRlaQ1Imh4bQNPdfths2U";
            static readonly string redirect_uri = "http://localhost";//or: "urn:ietf:wg:oauth:2.0:oob"
            static RestRequests.RestRequestsLimiter responseGetter = new RestRequests.RestRequestsLimiter();
            static string refreshToken = null, accessToken = null, tokenType = null;
            static DateTime expireTime = DateTime.Now;
            public static bool AccessTokenExpired { get { return expireTime >= DateTime.Now; } }
            class TemporaryClassForGetAccessTokenAsync
            {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
                public string access_token, refresh_token, token_type;
                public int expires_in;
#pragma warning restore 0649
            }
            static Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
            public static async Task MaintainAvailability(bool refreshAgain = false)
            {
                await semaphore.WaitAsync();
                try
                {
                    if (accessToken == null || (accessToken != null && refreshAgain))
                    {
                        var timeNow = DateTime.Now;
                        string str;
                        if (accessToken == null) str = await ExchangeCodeForTokens(await GetAuthorizationCode());
                        else str = await RefreshTokenAsync(refreshToken);
                        var obj = JsonConvert.DeserializeObject<TemporaryClassForGetAccessTokenAsync>(str);
                        if (accessToken == null) refreshToken = obj.refresh_token;
                        accessToken = obj.access_token;
                        tokenType = obj.token_type;
                        expireTime = timeNow.AddSeconds(obj.expires_in);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
            public static async Task<string> GetTokenTypeAsync(bool refreshAgain = false)
            {
                await MaintainAvailability(refreshAgain);
                return tokenType;
            }
            public static async Task<string> GetAccessTokenAsync(bool refreshAgain = false)
            {
                await MaintainAvailability(refreshAgain);
                return accessToken;
            }
            static async Task<string> RefreshTokenAsync(string refreshToken)
            {
                var body = $"client_secret={WebUtility.UrlEncode(client_secret)}" +
                    $"&grant_type=refresh_token" +
                    $"&refresh_token={WebUtility.UrlEncode(refreshToken)}" +
                    $"&client_id={WebUtility.UrlEncode(client_id)}";
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                HttpWebRequest request = HttpWebRequest.CreateHttp(token_uri);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers["Content-Length"] = bodyBytes.Length.ToString();
                request.Method = "POST";
                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                }
                using (var response = await responseGetter.GetHttpResponseAsync(request))
                {
                    using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            static async Task<string> ExchangeCodeForTokens(string authorizationCode)
            {
                var body = $"code={WebUtility.UrlEncode(authorizationCode)}" +
                    $"&redirect_uri={WebUtility.UrlEncode(redirect_uri)}" +
                    $"&client_id={WebUtility.UrlEncode(client_id)}" +
                    $"&client_secret={WebUtility.UrlEncode(client_secret)}" +
                    $"&scope=&grant_type=authorization_code";
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                HttpWebRequest request = HttpWebRequest.CreateHttp(token_uri);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers["Content-Length"] = bodyBytes.Length.ToString();
                request.Method = "POST";
                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                }
                using (var response = await responseGetter.GetHttpResponseAsync(request))
                {
                    using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            static async Task<string> GetAuthorizationCode()
            {
                var uri = auth_uri +
                    $"?redirect_uri={WebUtility.UrlEncode(redirect_uri)}" +
                    $"&prompt=consent&response_type=code&client_id={WebUtility.UrlEncode(client_id)}" +
                    $"&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fdrive&access_type=offline";
                string ans = null;
                for (int i = 0; ans == null||ans.StartsWith("/?error="); i++)
                {
                    SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0, 1);
                    var eventAction = new Action<string>((string eUrl) =>
                    {
                        var keyWord = $"{redirect_uri}";
                        //await MyLogger.Alert(e.Url);
                        if (eUrl.StartsWith(keyWord))
                        {
                            ans = eUrl.Substring(keyWord.Length);
                            var kw = "/?code=";
                            if (ans.StartsWith(kw)) ans = ans.Substring(kw.Length);
                            semaphoreSlim.Release();
                        }
                    });
                    await OpenWebWindowToGetAuthorizationCode(i == 0 ? "Sign in to your Google Drive, please" : "Signing in to Google Drive is required in order to get the data", uri, semaphoreSlim, eventAction);
                }
                return ans;
            }
        }
        /*public override async Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            var response= await base.GetHttpResponseAsync(request);
            if(response?.StatusCode==HttpStatusCode.Unauthorized)
            {
                MyLogger.Log("Http response: Unauthorized (401). May due to expired access token, refreshing...");
                MyLogger.Log(await LogHttpWebResponse(ans, true));
                MyLogger.Assert(Array.IndexOf(request.Headers.AllKeys, "Authorization") != -1);
                request.Headers["Authorization"] = "Bearer " + (await Drive.RefreshAccessTokenAsync());
                await Task.Delay(500);
            }
        }*/
    }
}
