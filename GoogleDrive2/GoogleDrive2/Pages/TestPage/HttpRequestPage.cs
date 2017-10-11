using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using GoogleDrive2.MyControls;
using System.ComponentModel;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.TestPage
{
    class HttpRequestPage : MyContentPage
    {
        public HttpRequestPage()
        {
            this.Title = "Http Request";
            this.Content = new HttpRequestView();
        }
        class HttpRequestView : MyGrid
        {
            MyEntry ETurl;
            AddableKeyValuePanel AKVfield, AKVheader;
            MyEditor EDbody, EDresponse;
            MyButton BTNsend;
            RestRequests.RestRequester requester = new RestRequests.RestRequester();
            async Task MultipartUploadExample()
            {
                ETurl.Text = "POST https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
                string json = "Content-Type: application/json; charset=UTF-8\n\n" +
                    "{\n" +
                    "\"name\":\"Hi.txt\"\n" +
                    //"\"parents\":[\"root\"]\n" +
                    "}";
                string fileContent = "Hey!";
                var body = "--abcde\n" +
                    $"{json}\n" +
                    "--abcde\n" +
                    "\n" +
                    $"{fileContent}\n" +
                    "--abcde--";
                //await MyLogger.Alert($"{body.Length}");
                EDbody.Text = body;
                await AKVheader.BLPmain.ClearAsync();
                await AddAuthorization();
                AddContentLength();
                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "multipart/related; charset=UTF-8; boundary=abcde"
                };
                foreach (var header in headers)
                {
                    AKVheader.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = header.Key, Value = header.Value });
                }
            }
            void AddGuid()
            {
                AKVfield.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = "Guid", Value = Guid.NewGuid().ToString() });
            }
            async Task AddAuthorization()
            {
                AKVheader.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = "Authorization", Value =$"{await RestRequests.RestRequestsAuthorizer.DriveAuthorizer.GetTokenTypeAsync()} {await RestRequests.RestRequestsAuthorizer.DriveAuthorizer.GetAccessTokenAsync()}" });
            }
            void AddContentLength()
            {
                AKVheader.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = "Content-Length", Value = $"{Encoding.UTF8.GetBytes(BodyTextToSend()).Length}" });
            }
            private void InitializeViews()
            {
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                {
                    var sp = new MyStackPanel (ScrollOrientation.Horizontal );
                    {
                        var btn = new MyButton { Text = "Multipart Upload" };
                        btn.Clicked += async delegate { await MultipartUploadExample(); };
                        sp.Children.Add(btn);
                    }
                    this.Children.Add(sp, 0, 0);
                }
                {
                    var gd = new MyGrid();
                    {
                        gd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        gd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        {
                            ETurl = new MyEntry { Text = "GET https://www.googleapis.com/drive/v3/files" };
                            gd.Children.Add(ETurl, 0, 0);
                        }
                        {
                            BTNsend = new MyButton { Text = "Send" };
                            gd.Children.Add(BTNsend, 1, 0);
                        }
                    }
                    this.Children.Add(gd, 0, 1);
                    MyGrid.SetColumnSpan(gd, 2);
                }
                {
                    AKVfield = new AddableKeyValuePanel("Fields");
                    AKVfield.AddButton("Fields", new Action(() =>
                    {
                        AKVfield.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = "fields", Value = "files(name,id,mimeType,md5Checksum)" });
                    }));
                    AKVfield.AddButton("Q", new Action(() =>
                    {
                        AKVfield.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = "q", Value = "name='hi'" });
                    }));
                    AKVfield.AddButton("Guid", new Action(() =>
                    {
                        AddGuid();
                    }));
                    this.Children.Add(AKVfield, 0, 2);
                }
                {
                    AKVheader = new AddableKeyValuePanel("Headers");
                    AKVheader.AddButton("Autorization", new Action(async () =>
                    {
                        await AddAuthorization();
                    }));
                    AKVheader.AddButton("Content-Length", new Action(() =>
                    {
                        AddContentLength();
                    }));
                    AKVheader.AddButton("Content-Type", new Action(() =>
                    {
                        AKVheader.BLPmain.PushFront(new KeyValueItemBarViewModel { Key = "Content-Type", Value = "application /json; charset=UTF-8" });
                    }));
                    this.Children.Add(AKVheader, 0, 3);
                }
                {
                    EDbody = new MyEditor();
                    this.Children.Add(EDbody, 1, 2);
                }
                {
                    EDresponse = new MyEditor();
                    this.Children.Add(EDresponse, 1, 3);
                }
            }
            private string BodyTextToSend()
            {
                return EDbody.Text.Replace('\r', '\n');
            }
            private async Task SendHttpRequest()
            {
                string url = ETurl.Text;
                {
                    bool isFirst = true;
                    //await MyLogger.Alert(url);
                    foreach (var field in AKVfield.BLPmain.ToList())
                    {
                        //await MyLogger.Alert($"Field: {field.Key} = {field.Value}");
                        url += (isFirst ? "?" : "&");
                        isFirst = false;
                        url += System.Net.WebUtility.UrlEncode(field.Key) + "=" + System.Net.WebUtility.UrlEncode(field.Value);
                    }
                }
                var blankPoint = url.IndexOf(' ');
                MyLogger.Assert(blankPoint != -1);
                MyHttpRequest request =new MyHttpRequest(url.Remove(blankPoint), url.Substring(blankPoint + 1));
                foreach (var header in AKVheader.BLPmain.ToList())
                {
                    //await MyLogger.Alert($"Header: {header.Key} = {header.Value}");
                    request.Headers[header.Key] = header.Value;
                }
                if (Array.IndexOf(request.Headers.AllKeys, "Content-Length") != -1)
                {
                    request.WriteBytes(Encoding.UTF8.GetBytes(BodyTextToSend()));
                }
                string result;
                try
                {
                    using (var response = await requester.GetHttpResponseAsync(request))
                    {
                        if (response == null)
                        {
                            result = "(Null Response)";
                        }
                        else
                        {
                            result = RestRequests.RestRequester.LogHttpWebResponse(response, true);
                        }
                    }
                }
                catch (Exception error)
                {
                    result = $"{error}";
                }
                EDresponse.Text = result;
            }
            private void RegisterEvents()
            {
                BTNsend.Clicked += async delegate
                {
                    BTNsend.IsEnabled = false;
                    await SendHttpRequest();
                    BTNsend.IsEnabled = true;
                };
            }
            public HttpRequestView()
            {
                InitializeViews();
                RegisterEvents();
            }
        }
        class AddableKeyValuePanel : MyGrid
        {
            public MyControls.BarsListPanel.BarsListPanel<KeyValueItemBar, KeyValueItemBarViewModel> BLPmain;
            MyButton BTNadd;
            MyLabel LBtitle;
            MyStackPanel SPbtn;
            public void AddButton(string text, Action action)
            {
                var btn = new MyButton { Text = text };
                btn.Clicked += delegate { action?.Invoke(); };
                SPbtn.Children.Add(btn);
            }
            private void InitializeViews()
            {
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                {
                    SPbtn = new MyStackPanel(Xamarin.Forms.ScrollOrientation.Horizontal);
                    {
                        BTNadd = new MyButton { Text = "Add" };
                        SPbtn.Children.Add(BTNadd);
                    }
                    this.Children.Add(SPbtn, 0, 0);
                }
                {
                    LBtitle = new MyLabel { Text = title };
                    this.Children.Add(LBtitle, 1, 0);
                }
                {
                    BLPmain = new MyControls.BarsListPanel.BarsListPanel<KeyValueItemBar, KeyValueItemBarViewModel>();
                    this.Children.Add(BLPmain, 0, 1);
                    MyGrid.SetColumnSpan(BLPmain, 2);
                }
            }
            private void RegisterEvents()
            {
                BTNadd.Clicked += delegate
                {
                    BLPmain.PushFront(new KeyValueItemBarViewModel());
                };
            }
            string title;
            public AddableKeyValuePanel(string _title)
            {
                this.title = _title;
                InitializeViews();
                RegisterEvents();
            }
        }
        class KeyValueItemBar : MyGrid, MyControls.BarsListPanel.IDataBindedView<KeyValueItemBarViewModel>
        {
            public event MyControls.BarsListPanel.DataBindedViewEventHandler<KeyValueItemBarViewModel> Appeared;
            public Func<Task> Disappearing { get; set; }
            public void Reset(KeyValueItemBarViewModel source)
            {
                if (this.BindingContext != null) (this.BindingContext as MyControls.BarsListPanel.MyDisposable).UnregisterDisposingEvents();
                this.BindingContext = source;
                if (source != null) source.Disposing = new Func<Task>(async () => { await Disappearing?.Invoke(); }); //MyDispossable will automatically unregister all Disposing events after disposed
                Appeared?.Invoke(this);
            }
            MyEntry ETkey, ETvalue;
            MyButton BTNcancel;
            public KeyValueItemBar()
            {
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40, GridUnitType.Absolute) });
                {
                    ETkey = new MyEntry();
                    ETkey.SetBinding(MyEntry.TextProperty, "Key", BindingMode.TwoWay);
                    this.Children.Add(ETkey, 0, 0);
                }
                {
                    ETvalue = new MyEntry();
                    ETvalue.SetBinding(MyEntry.TextProperty, "Value", BindingMode.TwoWay);
                    this.Children.Add(ETvalue, 1, 0);
                }
                {
                    BTNcancel = new MyButton { Text = "\u2716" };//✖
                    BTNcancel.Clicked += async delegate
                    {
                        if (this.BindingContext != null)// && await MyLogger.Ask($"Remove this item?\r\nKey: {ETkey.Text}\r\nValue: {ETvalue.Text}"))
                            {
                            await (this.BindingContext as MyControls.BarsListPanel.MyDisposable).OnDisposed();
                        }
                    };
                    this.Children.Add(BTNcancel, 2, 0);
                }
                System.Threading.SemaphoreSlim semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);
                this.Appeared += async (sender) =>
                {
                    this.Opacity = 0;
                    await semaphoreSlim.WaitAsync();
                        //this.Opacity = 1;
                        await this.FadeTo(1, 500);
                    lock (semaphoreSlim) semaphoreSlim.Release();
                };
                this.Disappearing = new Func<Task>(async () =>
                {
                    await semaphoreSlim.WaitAsync();
                        //this.Opacity = 0;
                        await this.FadeTo(0, 500);
                    lock (semaphoreSlim) semaphoreSlim.Release();
                });
            }
        }
        class KeyValueItemBarViewModel : MyControls.BarsListPanel.MyDisposable, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            string __Key__, __Value__;
            public string Key
            {
                get { return __Key__; }
                set
                {
                    if (__Key__ == value) return;
                    __Key__ = value;
                    OnPropertyChanged("Key");
                }
            }
            public string Value
            {
                get { return __Value__; }
                set
                {
                    if (__Value__ == value) return;
                    __Value__ = value;
                    OnPropertyChanged("Value");
                }
            }
        }
    }
}
