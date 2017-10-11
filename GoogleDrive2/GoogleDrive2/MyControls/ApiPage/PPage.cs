using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using GoogleDrive2.Api;
using Xamarin.Forms;
using System.Net;

namespace GoogleDrive2.MyControls.ApiPage
{
    class FieldsListView: MyContentView
    {
        public class KeyValueItemBar : MyGrid, BarsListPanel.IDataBindedView<KeyValueItemBarViewModel>
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
        public class KeyValueItemBarViewModel : MyControls.BarsListPanel.MyDisposable, INotifyPropertyChanged
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
        public BarsListPanel.BarsListPanel<KeyValueItemBar, KeyValueItemBarViewModel> listView;
        MyGrid GDmain;
        MyStackPanel SPmain;
        MyButton BTNadd;
        private void InitializeViews()
        {
            GDmain = new MyGrid();
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            {
                SPmain = new MyStackPanel(ScrollOrientation.Horizontal);
                {
                    BTNadd = new MyButton { Text = "Add" };
                    SPmain.Children.Add(BTNadd);
                }
                GDmain.Children.Add(SPmain, 0, 0);
            }
            {
                listView = new BarsListPanel.BarsListPanel<KeyValueItemBar, KeyValueItemBarViewModel>();
                GDmain.Children.Add(listView, 0, 1);
            }
            this.Content = GDmain;
        }
        private void RegisterEvents()
        {
            BTNadd.Clicked += delegate
              {
                  listView.PushFront(new KeyValueItemBarViewModel());
              };
        }
        public FieldsListView()
        {
            InitializeViews();
            RegisterEvents();
        }
        public async Task Update<P>(P v) where P : ParametersClass, new()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            ParametersClass.AddParameters(v, data,true);
            await listView.ClearAsync();
            foreach(var p in data)
            {
                listView.PushBack(new KeyValueItemBarViewModel { Key = p.Key, Value = p.Value });
            }
        }
    }
    class PPage: MyContentPage //where P : ParametersClass, new() where R:RequesterP<P>,new()
    {
        MyGrid GDmain,GDctrl;
        MyButton BTNsend;
        FieldsListView FLVmain;
        MyEditor EDmain;
        MyEntry ETuri;
        protected async Task Update<P,R>() where P : ParametersClass, new() where R:RequesterP<P>,new()
        {
            await FLVmain.Update(new P());
            var r = new R();
            ETuri.Text = $"{r.Method} {r.Uri}";
        }
        private void InitializeViews()
        {
            GDmain = new MyGrid();
            GDmain.ColumnDefinitions.Add(new Xamarin.Forms.ColumnDefinition { Width = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Star) });
            GDmain.ColumnDefinitions.Add(new Xamarin.Forms.ColumnDefinition { Width = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Star) });
            GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Auto) });
            GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Star) });
            {
                GDctrl = new MyGrid();
                GDctrl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GDctrl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                {
                    ETuri = new MyEntry();
                    GDctrl.Children.Add(ETuri, 0, 0);
                }
                {
                    BTNsend = new MyButton { Text = "Send" };
                    GDctrl.Children.Add(BTNsend, 1, 0);
                }
                GDmain.Children.Add(GDctrl, 0, 0);
                MyGrid.SetColumnSpan(GDctrl, 2);
            }
            {
                FLVmain = new FieldsListView();
                GDmain.Children.Add(FLVmain, 0, 1);
            }
            {
                EDmain = new MyEditor();
                GDmain.Children.Add(EDmain, 1, 1);
            }
            this.Content = GDmain;
        }
        RestRequests.RestRequester requester = new RestRequests.RestRequester();
        volatile int threadCnt=0;
        void UpdateBtnText()
        {
            if (threadCnt == 0) BTNsend.Text = "Send";
            else BTNsend.Text = $"Sending...({threadCnt})";
        }
        private void RegisterEvents()
        {
            BTNsend.Clicked += async delegate
              {
                  threadCnt++; UpdateBtnText();
                  var uri = ETuri.Text;
                  var request = new RequesterRaw(uri.Remove(uri.IndexOf(' ')), uri.Substring(uri.IndexOf(' ') + 1), true);
                  foreach (var p in FLVmain.listView.ToList())if(!string.IsNullOrEmpty(p.Value))
                  {
                      //await MyLogger.Alert($"Header: {header.Key} = {header.Value}");
                      request.Parameters[p.Key] = p.Value;
                  }
                  using (var response = await request.GetHttpResponseAsync())
                  {
                      EDmain.Text = RestRequests.RestRequester.LogHttpWebResponse(response, true);
                  }
                  threadCnt--; UpdateBtnText();
              };
        }
        public PPage()
        {
            InitializeViews();
            RegisterEvents();
        }
    }
}
