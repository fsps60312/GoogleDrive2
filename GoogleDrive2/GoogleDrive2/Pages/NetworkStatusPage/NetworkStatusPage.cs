using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.ComponentModel;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class NetworkStatusBarViewModel: MyControls.BarsListPanel.MyDisposable,INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public MyHttpRequest Request { get; private set; }
        private string __Uri__;
        public string Uri
        {
            get { return __Uri__; }
            set
            {
                if (__Uri__ == value) return;
                __Uri__ = value;
                OnPropertyChanged("Uri");
            }
        }
        private string __Status__;
        public string Status
        {
            get { return __Status__; }
            set
            {
                if (__Status__ == value) return;
                __Status__ = value;
                OnPropertyChanged("Status");
            }
        }
        private Color __Color__ = Color.Default;
        public Color Color
        {
            get { return __Color__; }
            set
            {
                if (__Color__ == value) return;
                __Color__ = value;
                OnPropertyChanged("Color");
            }
        }
        private System.Windows.Input.ICommand __Clicked__;
        public System.Windows.Input.ICommand Clicked
        {
            get { return __Clicked__; }
            set
            {
                if (__Clicked__ == value) return;
                __Clicked__ = value;
                OnPropertyChanged("Clicked");
            }
        }
        List<TimeSpan> times = new List<TimeSpan>();
        string GetTimes()
        {
            return $" → {times.Last().TotalMilliseconds} ms ";
            //return string.Join("→", times.Select((v) => { return $"{v.TotalMilliseconds} ms"; }));
        }
        bool __CloseCounter__ = true;
        bool CloseCounter
        {
            get { return __CloseCounter__; }
            set
            {
                if (__CloseCounter__ == value) return;
                __CloseCounter__ = value;
                semaphore.SetThreadLimit(semaphore.ThreadCountLimitRequest + (value ? 1 : -1));
            }
        }
        Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
        public NetworkStatusBarViewModel(MyHttpRequest request)
        {
            Request = request;
            Status = "Unknown";
            Uri = Request.Method + " " + Request.Uri;
            Clicked = new Command(async () =>
            {
                CloseCounter = false;
                if(await MyLogger.Ask(Status+ Environment.NewLine + Environment.NewLine + Environment.NewLine + Request.ToString() + Environment.NewLine + Environment.NewLine + Environment.NewLine+"That's all, clear this request history?"))
                {
                    CloseCounter = true;
                }
              });
            var time = DateTime.Now;
            Request.Started += delegate { times.Add(DateTime.Now-time); Status +=GetTimes()+ "(1/7) Started"; };
            Request.Writing += delegate { times.Add(DateTime.Now - time); Status+= GetTimes() + "(2/7) Writing"; };
            Request.Requesting += delegate { times.Add(DateTime.Now - time); Status+= GetTimes() + "(3/7) Requesting"; };
            Request.Responded += (r)=>
            {
                times.Add(DateTime.Now - time); Status += GetTimes() + "(4/7) Responded";
                switch(r?.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:Color = Color.LightGreen;break;
                    case System.Net.HttpStatusCode.PartialContent:Color = Color.YellowGreen;break;
                    default:
                        {
                            Color = Color.Red;
                            CloseCounter = false;
                            break;
                        }
                }
            };
            Request.Receiving += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(5/7) Receiving"; };
            Request.Received += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(6/7) Received"; };
            Request.Finished +=async delegate
            {
                times.Add(DateTime.Now - time);
                Status +=GetTimes()+ "(7/7) Finished";
                for(int i=0;i<10;i++)
                {
                    await semaphore.WaitAsync();
                    await Task.Delay(2000);
                    semaphore.Release();
                    Status += '.';
                }
                Status += "🙋";
                await Task.Delay(2000);
                await this.OnDisposed();
            };
        }
    }
    class NetworkStatusBar:MyGrid, MyControls.BarsListPanel.IDataBindedView<NetworkStatusBarViewModel>
    {
        public event MyControls.BarsListPanel.DataBindedViewEventHandler<NetworkStatusBarViewModel> Appeared;
        public Func<Task> Disappearing { get; set; }
        public void Reset(NetworkStatusBarViewModel source)
        {
            if (this.BindingContext != null) (this.BindingContext as MyControls.BarsListPanel.MyDisposable).UnregisterDisposingEvents();
            this.BindingContext = source;
            if (source != null) source.Disposing = new Func<Task>(async () => { await Disappearing?.Invoke(); }); //MyDispossable will automatically unregister all Disposing events after disposed
            Appeared?.Invoke(this);
        }
        MyLabel LBuri,LBstatus;
        MyScrollView SVstatus;
        MyButton BTNdetail;
        public NetworkStatusBar()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            {
                LBuri = new MyLabel();
                LBuri.SetBinding(MyLabel.TextProperty, "Uri");
                LBuri.SetBinding(MyLabel.BackgroundColorProperty, "Color");
                LBuri.LineBreakMode = LineBreakMode.MiddleTruncation;
                this.Children.Add(LBuri, 0, 0);
            }
            {
                SVstatus = new MyScrollView { Orientation = ScrollOrientation.Horizontal };
                SVstatus.LayoutChanged += async delegate { await SVstatus.ScrollToAsync(LBstatus, ScrollToPosition.End, true); };
                {
                    LBstatus = new MyLabel();
                    LBstatus.SetBinding(MyLabel.TextProperty, "Status");
                    LBstatus.LineBreakMode = LineBreakMode.HeadTruncation;
                    SVstatus.Content = LBstatus;
                }
                this.Children.Add(SVstatus, 1, 0);
            }
            {
                BTNdetail = new MyButton { Text = Constants.Icons.Info };
                BTNdetail.SetBinding(MyButton.CommandProperty, "Clicked");
                this.Children.Add(BTNdetail, 2, 0);
            }
            System.Threading.SemaphoreSlim semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);
            this.Appeared += async (sender) =>
            {
                this.Opacity = 0;
                await semaphoreSlim.WaitAsync();
                await this.FadeTo(1, 500);
                lock (semaphoreSlim) semaphoreSlim.Release();
            };
            this.Disappearing = new Func<Task>(async () =>
            {
                await semaphoreSlim.WaitAsync();
                await this.FadeTo(0, 500);
                lock (semaphoreSlim) semaphoreSlim.Release();
            });
        }
    }
    class NetworkStatusPanel : MyControls.BarsListPanel.BarsListPanel<NetworkStatusBar, NetworkStatusBarViewModel>
    {
        public NetworkStatusPanel()
        {
            MyHttpRequest.NewRequestCreated += (r) =>
            {
                this.PushBack(new NetworkStatusBarViewModel(r));
            };
        }
    }
    class NetworkStatusPage:MyContentPage
    {
        public NetworkStatusPage()
        {
            this.Title = "Network Status";
            this.Content = new NetworkStatusPanel();
        }
    }
}
