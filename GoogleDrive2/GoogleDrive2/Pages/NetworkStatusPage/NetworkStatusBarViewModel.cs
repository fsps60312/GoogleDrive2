using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    partial class NetworkStatusWithSpeedBarViewModel : PausableNetworkStatusBarViewModel
    {
        #region Properties
        string __Speed__ = null;
        Xamarin.Forms.ImageSource __SpeedGraph__ = null;
        public Xamarin.Forms.ImageSource SpeedGraph
        {
            get { return __SpeedGraph__; }
            set
            {
                if (value == __SpeedGraph__) return;
                __SpeedGraph__ = value;
                OnPropertyChanged("SpeedGraph");
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
        #endregion
        SpeedMaintainer speedMaintainer = new SpeedMaintainer();
        public List<Tuple<double, double>> SpeedHistory = new List<Tuple<double, double>>();
        protected void OnSpeedDataAdded(long byteCount)
        {
            speedMaintainer.Add(byteCount);
        }
        protected NetworkStatusWithSpeedBarViewModel():base()
        {
            var frequencyLimiter = new Libraries.FrequentExecutionLimiter(0.5);
            speedMaintainer.SpeedUpdated += (v) => frequencyLimiter.Execute(() =>
            {
                if (v == 0) Speed = null;
                else Speed = $"{ByteCountToString((long)v, 3)}/s";
                lock (SpeedHistory)
                {
                    SpeedHistory.Add(new Tuple<double, double>(Progress, v));
                }
                //MyLogger.Debug(Progress.ToString());
                SpeedGraph = Xamarin.Forms.ImageSource.FromStream(new Func<System.IO.Stream>(() =>
                {
                    const int width = 500, height = 20;
                    List<Tuple<double, double>> points = new List<Tuple<double, double>>();
                    lock (SpeedHistory)
                    {
                        if (SpeedHistory.Count <= width * 2)
                        {
                            foreach (var p in SpeedHistory) points.Add(p);
                        }
                        else
                        {
                            for (int i = 0; i < width * 3; i++) points.Add(SpeedHistory[i * SpeedHistory.Count / (width * 3)]);
                        }
                    }
                    return ImageProcessor.GetImageStream(width, height, points);
                }));
            });
        }
    }
    partial class PausableNetworkStatusBarViewModel : NetworkStatusBarViewModel
    {
        #region Properties
        System.Windows.Input.ICommand __PauseClicked__;
        string __PauseButtonText__ = Constants.Icons.Pause;
        bool __PauseButtonEnabled__ = true;
        public bool PauseButtonEnabled
        {
            get { return __PauseButtonEnabled__; }
            set
            {
                if (value == __PauseButtonEnabled__) return;
                __PauseButtonEnabled__ = value;
                OnPropertyChanged("PauseButtonEnabled");
            }
        }
        public string PauseButtonText
        {
            get { return __PauseButtonText__; }
            set
            {
                if (value == __PauseButtonText__) return;
                __PauseButtonText__ = value;
                OnPropertyChanged("PauseButtonText");
            }
        }
        public System.Windows.Input.ICommand PauseClicked
        {
            get { return __PauseClicked__; }
            set
            {
                if (value == __PauseClicked__) return;
                __PauseClicked__ = value;
                OnPropertyChanged("PauseClicked");
            }
        }
        #endregion
        protected void OnCompleted(bool success)
        {
            if (success)
            {
                PauseButtonEnabled = false;
                OnMessageAppended($"{Constants.Icons.Completed} Completed");
                Icon = Constants.Icons.Completed;
            }
            else
            {
                OnMessageAppended($"{Constants.Icons.Pause} Paused");
                Icon = Constants.Icons.Pause;
            }
            PauseButtonText = Constants.Icons.Play;
        }
        protected void OnPaused()
        {
            Icon = Constants.Icons.Pause;
        }
        protected void OnPausing()
        {
            PauseButtonText = Constants.Icons.Play;
            Icon = Constants.Icons.Pausing;
        }
        protected void OnStarted()
        {
            PauseButtonText = Constants.Icons.Pause;
            Icon = Constants.Icons.Hourglass;
        }
        protected PausableNetworkStatusBarViewModel():base() { }
    }
    partial class NetworkStatusBarViewModel : MyControls.BarsListPanel.MyDisposable
    {
        protected int IndentSize = 20;
        #region Properties
        double __Progress__ = 0;
        string __Icon__ = Constants.Icons.Initial;
        string __Nmae__ = null;
        string __Info__ = null;
        System.Windows.Input.ICommand __InfoClicked__;
        bool __InfoEnabled__ = true;
        string __TimeRemaining__ = null; // Estimated Remaining Time
        string __TimePassed__ = null;
        int __Indent__= 0;
        Xamarin.Forms.Thickness __Margin__ = new Xamarin.Forms.Thickness();
        public Xamarin.Forms.Thickness Margin
        {
            get { return __Margin__; }
            set
            {
                if (value == __Margin__) return;
                __Margin__ = value;
                OnPropertyChanged("Margin");
            }
        }
        public int Indent
        {
            get { return __Indent__; }
            set
            {
                if (value == __Indent__) return;
                __Indent__ = value;
                Margin = new Xamarin.Forms.Thickness(Indent * IndentSize, 0, 0, 0);
                OnPropertyChanged("Indent");
            }
        }
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
        public virtual double Progress
        {
            get { return __Progress__; }
            set
            {
                if (__Progress__ == value) return;
                __Progress__ = value;
                OnPropertyChanged("Progress");
            }
        }
        #endregion
        public class ProgressTextValueConverter : Xamarin.Forms.IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return $"{((double)value * 100).ToString("F3")}%";
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        TimeRemainingMaintainer timeRemainingMaintainer = new TimeRemainingMaintainer();
        List<string> messages = new List<string>();
        protected void OnMessageAppended(string msg)
        {
            messages.Add(msg);
            Info = messages.Count == 1 ? msg : $"({messages.Count}) {msg}";
        }
        protected static string ByteCountToString(long byteCount, int precision)
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
        protected NetworkStatusBarViewModel()
        {
            InfoClicked = new Xamarin.Forms.Command(async () =>
            {
                InfoEnabled = false;
                await MyLogger.Alert(messages.Count == 0 ? $"{Constants.Icons.Info} No messages" : string.Join(Environment.NewLine, messages.Reverse<string>()));
                InfoEnabled = true;
            });
            timeRemainingMaintainer.TimeRemainingUpdated += (v) =>
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
            PropertyChanged += (o, v) =>
              {
                  if (v.PropertyName == "Progress") timeRemainingMaintainer.Add(Progress);
              };
        }
    }
}