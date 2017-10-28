using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class FileUploadBarsList:MyControls.BarsListPanel.BarsListPanel<FileUploadBar,FileUploadBarViewModel>
    {
        public FileUploadBarsList()
        {
            Local.File.Uploader.NewUploaderCreated += (uploader) =>
              {
                  this.PushBack(new FileUploadBarViewModel(uploader));
              };
        }
    }
    class FileUploadBar:MyControls.BarsListPanel.DataBindedGrid<FileUploadBarViewModel>
    {
        MyLabel LBicon, LBname, LBuploaded,LBtotal,LBpercentage,LBspeed,LBtimeRemaining,LBtimePassed;
        MyButton BTNinfo;
        MyProgressBar PBprogress;
        private void SetBindings()
        {
            LBicon.SetBinding(MyLabel.TextProperty, "Icon");
            LBname.SetBinding(MyLabel.TextProperty, "Name");
            LBuploaded.SetBinding(MyLabel.TextProperty, "Uploaded");
            LBtotal.SetBinding(MyLabel.TextProperty, "Total");
            LBpercentage.SetBinding(MyLabel.TextProperty, "Percentage");
            LBspeed.SetBinding(MyLabel.TextProperty, "Speed");
            LBtimeRemaining.SetBinding(MyLabel.TextProperty, "TimeRemaining");
            LBtimePassed.SetBinding(MyLabel.TextProperty, "TimePassed");
            BTNinfo.SetBinding(MyButton.TextProperty, "Info");
            BTNinfo.SetBinding(MyButton.CommandProperty, "InfoClicked");
            BTNinfo.SetBinding(MyButton.IsEnabledProperty, "InfoEnabled");
            PBprogress.SetBinding(MyProgressBar.ProgressProperty, "Progress");
        }
        private void ArrangeViews()
        {
            this.RowSpacing = 0;
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Absolute) });//icon
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//name
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//percentage, speed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//uploaded, time passed
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) });//total, time remaining
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });//info
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Absolute) });
            {
                this.Children.Add(PBprogress, 3, 0);
                MyGrid.SetColumnSpan(PBprogress, 2);
                this.Children.Add(LBicon, 0, 0);
                MyGrid.SetRowSpan(LBicon, 2);
                this.Children.Add(LBname, 1, 0);
                MyGrid.SetRowSpan(LBname, 2);
                this.Children.Add(LBpercentage, 2, 0);
                this.Children.Add(LBspeed, 2, 1);
                this.Children.Add(LBtimePassed, 3, 0);
                this.Children.Add(LBuploaded, 3, 1);
                this.Children.Add(LBtimeRemaining, 4, 0);
                this.Children.Add(LBtotal, 4, 1);
                this.Children.Add(BTNinfo, 5, 0);
                MyGrid.SetRowSpan(BTNinfo, 2);
            }
        }
        private void InitializeViews()
        {
            {
                LBicon = new MyLabel();
                LBname = new MyLabel();
                LBuploaded = new MyLabel();
                LBtotal = new MyLabel();
                LBpercentage = new MyLabel();
                LBspeed = new MyLabel();
                LBtimeRemaining = new MyLabel();
                LBtimePassed = new MyLabel();
                BTNinfo = new MyButton();
                PBprogress = new MyProgressBar();
            }
        }
        public FileUploadBar()
        {
            InitializeViews();
            ArrangeViews();
            SetBindings();
        }
    }
    class FileUploadBarViewModel:MyControls.BarsListPanel.MyDisposable
    {
        const double ProgressHistoryLifeTime = 2;
        double __Progress__ = 0;
        string __Icon__ = Constants.Icons.Initial;
        string __Nmae__ = null;
        string __Info__ = null;
        System.Windows.Input.ICommand __InfoClicked__;
        bool __InfoEnabled__ = true;
        string __Uploaded__ = null;
        string __Total__ = null;
        string __Percentage__ = null;
        string __Speed__ = null;
        string __TimeRemaining__ = null; // Estimated Remaining Time
        string __TimePassed__ = null;
        public string TimePassed
        {
            get { return __TimePassed__; }
            set
            {
                if (value == __TimePassed__) return;
                __TimePassed__ = value;
                OnPropertyChanged("TimePassed");
            }
        }
        public string TimeRemaining
        {
            get { return __TimeRemaining__; }
            set
            {
                if (value == __TimeRemaining__) return;
                __TimeRemaining__ = value;
                OnPropertyChanged("TimeRemaining");
            }
        }
        public string Speed
        {
            get { return __Speed__; }
            set
            {
                if (value == __Speed__) return;
                __Speed__ = value;
                OnPropertyChanged("Speed");
            }
        }
        public string Percentage
        {
            get { return __Percentage__; }
            set
            {
                if (value == __Percentage__) return;
                __Percentage__ = value;
                OnPropertyChanged("Percentage");
            }
        }
        public string Total
        {
            get { return __Total__; }
            set
            {
                if (value == __Total__) return;
                __Total__ = value;
                OnPropertyChanged("Total");
            }
        }
        public string Uploaded
        {
            get { return __Uploaded__; }
            set
            {
                if (value == __Uploaded__) return;
                __Uploaded__ = value;
                OnPropertyChanged("Uploaded");
            }
        }
        public bool InfoEnabled
        {
            get { return __InfoEnabled__; }
            set
            {
                if (__InfoEnabled__ == value) return;
                __InfoEnabled__ = value;
                OnPropertyChanged("InfoEnabled");
            }
        }
        public System.Windows.Input.ICommand InfoClicked
        {
            get { return __InfoClicked__; }
            set
            {
                if (__InfoClicked__ == value) return;
                __InfoClicked__ = value;
                OnPropertyChanged("InfoClicked");
            }
        }
        public string Info
        {
            get { return __Info__; }
            set
            {
                if (__Info__ == value) return;
                __Info__ = value;
                OnPropertyChanged("Info");
            }
        }
        public string Name
        {
            get { return __Nmae__; }
            set
            {
                if (__Nmae__ == value) return;
                __Nmae__ = value;
                OnPropertyChanged("Name");
            }
        }
        public string Icon
        {
            get { return __Icon__; }
            set
            {
                if (__Icon__ == value) return;
                __Icon__ = value;
                OnPropertyChanged("Icon");
            }
        }
        public double Progress
        {
            get { return __Progress__; }
            set
            {
                if (__Progress__ == value) return;
                __Progress__ = value;
                OnPropertyChanged("Progress");
            }
        }
        List<string> messages = new List<string>();
        private void WhenMessageAppended(string msg)
        {
            messages.Add(msg);
            Info =messages.Count==1? msg:$"({messages.Count}) {msg}";
        }
        private string ByteCountToString(long byteCount,int precision)
        {
            const double bound = 999;
            double v = byteCount;
            Tuple<double, string> ans;
            if (v <= bound) ans = new Tuple<double, string>(v, "B");
            else if ((v /= 1024) <= bound) ans = new Tuple<double, string>(v, "KiB");
            else if ((v /= 1024) <= bound) ans = new Tuple<double, string>(v, "MiB");
            else if ((v /= 1024) <= bound) ans = new Tuple<double, string>(v, "GiB");
            else
            {
                v /= 1024;
                ans = new Tuple<double, string>(v, "TiB");
            }
            return $"{ans.Item1.ToString($"F{precision}")/*.TrimEnd('0').TrimEnd('.')*/} {ans.Item2}";
        }
        class ProgressHistoryMaintainer
        {
            public event Libraries.Events.MyEventHandler<double> SpeedUpdated;
            public event Libraries.Events.MyEventHandler<TimeSpan>TimeRemainUpdated;
            Tuple<DateTime, double, long> record = new Tuple<DateTime, double, long>(DateTime.Now, 0, 0);
            Queue<Tuple<DateTime, double, long>> history = new Queue<Tuple<DateTime, double, long>>();
            int currentTrackId = 0;
            private void Update()
            {
                lock (history)
                {
                    MyLogger.Assert(history.Count > 0);
                    Tuple<double, TimeSpan> args;
                    var b = history.Last();
                    var a = history.Peek();
                    var sec = (b.Item1 - a.Item1).TotalSeconds;
                    if (sec == 0) args = new Tuple<double, TimeSpan>(0, new TimeSpan());
                    else args = new Tuple<double, TimeSpan>(
                        (b.Item3 - a.Item3) / sec,
                        new TimeSpan((long)((100 - b.Item2) / (b.Item2 - a.Item2) * sec * 1000 * 1000 * 10)));
                    MyLogger.Debug($"{a.Item2} {b.Item2}");
                    SpeedUpdated?.Invoke(args.Item1);
                    TimeRemainUpdated?.Invoke(args.Item2);
                }
            }
            private double MaintainHistory()
            {
                lock (history)
                {
                    MyLogger.Assert(history.Count > 0);
                    while (history.Count > 1 && (DateTime.Now - history.ElementAt(1).Item1).TotalSeconds >= ProgressHistoryLifeTime)
                    {
                        history.Dequeue();
                    }
                    if (history.Count == 1) return double.PositiveInfinity;
                    return (history.ElementAt(1).Item1.AddSeconds(ProgressHistoryLifeTime) - DateTime.Now).TotalMilliseconds;
                }
            }
            private async void StartTrack()
            {
                int id = System.Threading.Interlocked.Increment(ref currentTrackId);
                for (double timeToWait = 0; currentTrackId == id;)
                {
                    timeToWait = MaintainHistory();
                    Update();
                    if (double.IsPositiveInfinity(timeToWait)) return;
                    if (timeToWait > 0) await Task.Delay((int)timeToWait);
                }
            }
            public void Add(Tuple<DateTime, double, long> progress)
            {
                lock (history)
                {
                    history.Enqueue(progress);
                    StartTrack();
                }
            }
        }
        ProgressHistoryMaintainer progressHistoryMaintainer = new ProgressHistoryMaintainer();
        void SetStatusText(Tuple<long,long>progress)
        {
            var percent = (double)progress.Item1 * 100 / progress.Item2;
            progressHistoryMaintainer.Add(new Tuple<DateTime, double, long>(DateTime.Now, percent, progress.Item1));
            Percentage = $"{percent.ToString("F3")}%";
            Uploaded = ByteCountToString(progress.Item1, 3);
            Total = ByteCountToString(progress.Item2, 3);
        }
        public FileUploadBarViewModel(Local.File.Uploader up)
        {
            Name = up.F.Name;
            up.Completed += delegate
            {
                WhenMessageAppended($"{Constants.Icons.Completed} Completed");
                Icon = Constants.Icons.Completed;
                if(Progress==0)
                {
                    Progress = 1;
                    WhenMessageAppended($"{Constants.Icons.Info} This is an Empty File");
                }
            };
            up.MessageAppended += (msg) => { WhenMessageAppended(msg); };
            up.Paused += delegate { Icon = Constants.Icons.Pause; };
            up.Pausing += delegate { Icon = Constants.Icons.Pausing; };
            up.Started += delegate { Icon = Constants.Icons.Hourglass; };
            up.ProgressChanged += (p) =>
              {
                  SetStatusText(p);
                  if (p.Item2 == 0) Progress = 0;
                  else Progress = (double)p.Item1 / p.Item2;
              };
            InfoClicked = new Xamarin.Forms.Command(async () =>
              {
                  InfoEnabled = false;
                  await MyLogger.Alert(messages.Count==0?$"{Constants.Icons.Info}No messages": string.Join(Environment.NewLine, messages));
                  InfoEnabled = true;
              });
            progressHistoryMaintainer.SpeedUpdated += (v) =>
              {
                  if (v == 0) Speed = null;
                  else Speed = $"{ByteCountToString((long)v, 3)}/s";
              };
            progressHistoryMaintainer.TimeRemainUpdated += (v) =>
              {
                  if (v.Ticks == 0) TimeRemaining = null;
                  else
                  {
                      var sh = v.Hours.ToString("D2");
                      var sm = v.Minutes.ToString("D2");
                      var ss = v.Seconds.ToString("D2");
                      if (sh != "00") TimeRemaining = $"{sh}:{sm}:{ss}";
                      else if (sm != "00") TimeRemaining = $"{sm}:{ss}";
                      else TimeRemaining = $"{ss}";
                  }
              };
        }
    }
    class FileUploadPage:MyContentPage
    {
        public FileUploadPage()
        {
            this.Title = "File Upload";
            this.Content = new FileUploadBarsList();
        }
    }
}
