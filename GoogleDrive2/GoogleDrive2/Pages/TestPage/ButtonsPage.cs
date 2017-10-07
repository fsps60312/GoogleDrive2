using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Pages.TestPage
{
    class ButtonsPage:MyContentPage
    {
        MyStackPanel SPmain;
        class TemporaryClass
        {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
            public string id, name, mimeType,md5Checksum;
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
        }
        private void AddButtons()
        {
            AddButton("list response desearialize", new Func<Task>(async () =>
             {
                 var r = new Api.Files.ListRequest();
                 r.Parameters.fields = "nextPageToken,incompleteSearch,files(id,name,mimeType,md5Checksum)";
                 var response = await r.GetHttpResponseAsync();
                 var text = await r.GetResponseTextAsync(response);
                 //await MyLogger.Alert(text);
                 {
                     var result = JsonConvert.DeserializeObject<Api.Files.ListRequest.ListResponse<object>>(text);
                     await MyLogger.Alert(JsonConvert.SerializeObject(result));
                 }
                 {
                     var result = JsonConvert.DeserializeObject<Api.Files.ListRequest.ListResponse<TemporaryClass>>(text);
                     await MyLogger.Alert(JsonConvert.SerializeObject(result));
                 }
             }));
        }
        private void AddButton(string name,Func<Task>func)
        {
            var btn = new MyButton { Text = name };
            btn.Clicked += async delegate { await func(); };
            SPmain.Children.Add(btn);
        }
        private void InitializeViews()
        {
            this.Title = "Buttons";
            SPmain = new MyStackPanel(Xamarin.Forms.ScrollOrientation.Vertical);
            this.Content = SPmain;
        }
        public ButtonsPage()
        {
            InitializeViews();
            AddButtons();
        }
    }
}
