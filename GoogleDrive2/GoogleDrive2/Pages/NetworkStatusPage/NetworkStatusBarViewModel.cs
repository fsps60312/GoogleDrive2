using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.ComponentModel;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class NetworkStatusBarViewModel : MyControls.BarsListPanel.MyDisposable, INotifyPropertyChanged
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
        private string __Icon__ = Constants.Icons.Info;
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
        private double __Progress__;
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
        List<TimeSpan> times = new List<TimeSpan>();
        string GetTimes()
        {
            return $" → {times.Last().TotalMilliseconds} ms ";
            //return string.Join("→", times.Select((v) => { return $"{v.TotalMilliseconds} ms"; }));
        }
        DateTime disposeTime = DateTime.MaxValue;
        double timerSpan = 100000;
        void CancelTimer()
        {
            Icon = Constants.Icons.Info;
            disposeTime = DateTime.MaxValue;
        }
        void SetTimer(double secs)
        {
            Icon = Constants.Icons.Info;
            disposeTime = DateTime.Now;
            timerSpan = secs * 1000;
        }
        async void TriggerDisposeTimer()
        {
            if (disposeTime == DateTime.MaxValue) return;
            var time = disposeTime;
            double timeSpan = timerSpan;
            for (int i = 0; i < 12; i++)
            {
                if (disposeTime != time) return;
                Icon = Constants.Icons.Timers.Substring(i * 2, 2);
                await Task.Delay((int)(timeSpan / 12));
            }
            if (disposeTime != time) return;
            await this.OnDisposed();
        }
        bool inDialog = false;
        public NetworkStatusBarViewModel(MyHttpRequest request)
        {
            Request = request;
            Status = "Unknown";
            Uri = Request.Method + " " + Request.Uri;
            Clicked = new Command(async () =>
            {
                inDialog = true;
                try
                {
                    CancelTimer();
                    if (await MyLogger.Ask(Status + Environment.NewLine + Environment.NewLine + Environment.NewLine + Request.ToString() + Environment.NewLine + Environment.NewLine + Environment.NewLine + "That's all, clear this request history?"))
                    {
                        SetTimer(1);
                        TriggerDisposeTimer();
                    }
                }
                finally { inDialog = false; }
            });
            var time = DateTime.Now;
            Request.ProgressChanged += (p) =>
            {
                if (p.Item2.HasValue)
                {
                    if (p.Item2.Value == 0) Progress = 1;
                    else Progress = (double)p.Item1 / p.Item2.Value;
                }
                else
                {
                    var v = (double)p.Item1;
                    while (v > 1) v /= 2;
                    Progress = v;
                }
            };
            Request.Started += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(1/7) Started"; };
            Request.Writing += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(2/7) Writing"; };
            Request.Requesting += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(3/7) Requesting"; };
            Request.Responded += (r) =>
            {
                times.Add(DateTime.Now - time); Status += GetTimes() + "(4/7) Responded";
                Uri = $"({r?.StatusCode})" + Uri;
                switch (r?.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        {
                            Color = Color.GreenYellow;
                            if (!inDialog) SetTimer(10);
                            break;
                        }
                    default:
                        {
                            bool isResumeIncomplete = (int)r?.StatusCode == 308;
                            Color = isResumeIncomplete ? Color.Gold : Color.Red;
                            if (!inDialog)
                            {
                                if (isResumeIncomplete) SetTimer(2);
                                else CancelTimer();
                            }
                            break;
                        }
                }
            };
            Request.Receiving += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(5/7) Receiving"; };
            Request.Received += delegate { times.Add(DateTime.Now - time); Status += GetTimes() + "(6/7) Received"; };
            Request.Finished += delegate
            {
                times.Add(DateTime.Now - time);
                Status += GetTimes() + "(7/7) Finished";
                TriggerDisposeTimer();
            };
        }
    }
}
