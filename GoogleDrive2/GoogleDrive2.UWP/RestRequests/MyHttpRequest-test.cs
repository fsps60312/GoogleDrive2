using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Windows.Networking.NetworkOperators;

namespace GoogleDrive2.UWP
{
    //public class WebDependency : IWebDependency
    //{
    //    public HttpWebRequest GetWebRequest(string uri)
    //    {
    //        var request = WebRequest.Create(uri) as HttpWebRequest;
    //        request.SendChunked = true;
    //        request.AllowWriteStreamBuffering = false;

    //        return request;

    //    }

    //}
    public partial class MyHttpRequest2
    {
        async void DisableBuffering(HttpWebRequest request)
        {
            var handler = new System.Net.Http.Handlers.ProgressMessageHandler();
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient(handler, true);
            var h = new System.Net.Http.HttpRequestMessage();
            //h.Content=System.Net.Http.content
            var r=await httpClient.SendAsync(h);
        }
    }
}
