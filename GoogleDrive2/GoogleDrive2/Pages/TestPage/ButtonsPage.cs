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
        class TemporaryClass2
        {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
            public string sa = "a";
            public List<string> lsb = new List<string> { "aaa", "bbb", "ccc" };
            public string sb = "b";
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
        }
        void AddButton2()
        {
            AddButton("serialize List<string>", new Func<Task>(async () =>
            {
                {
                    var v = new TemporaryClass2();
                    await MyLogger.Alert(JsonConvert.SerializeObject(v));
                }
                {
                    var v = new Api.Files.FullCloudFileMetadata
                    {
                        name = "File name",
                        parents = new List<string> { "parent A", "parent B", "parent C" }
                    };
                    await MyLogger.Alert(JsonConvert.SerializeObject(v, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                }
            }));
        }
        class TemporaryClass1
        {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
            public string id, name, mimeType,md5Checksum;
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
        }
        void AddButton1()
        {
            AddButton("list response desearialize", new Func<Task>(async () =>
            {
                var r = new Api.Files.ListRequest();
                r.Parameters.fields = "nextPageToken,incompleteSearch,files(id,name,mimeType,md5Checksum)";
                using (var response = await r.GetHttpResponseAsync())
                {
                    var text =await r.GetResponseTextAsync(response);
                    //await MyLogger.Alert(text);
                    {
                        var result = JsonConvert.DeserializeObject<Api.Files.ListRequest.ListResponse<object>>(text);
                        await MyLogger.Alert(JsonConvert.SerializeObject(result));
                    }
                    {
                        var result = JsonConvert.DeserializeObject<Api.Files.ListRequest.ListResponse<TemporaryClass1>>(text);
                        await MyLogger.Alert(JsonConvert.SerializeObject(result));
                    }
                }
            }));
        }
        private void AddButtons()
        {
            AddButton1();
            AddButton2();
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
