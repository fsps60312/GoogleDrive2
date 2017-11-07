using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    partial class NetworkStatusBarViewModel
    {
        protected class TimeRemainingMaintainer : ProgressHistoryMaintainer
        {
            public event Libraries.Events.MyEventHandler<TimeSpan> TimeRemainingUpdated;
            protected override void Update()
            {
                var ep = GetEndpoints();
                var a = ep.Item1;
                var b = ep.Item2;
                var sec = (b.Item1 - a.Item1).TotalSeconds;
                //MyLogger.Debug($"{a.Item2} {b.Item2}");
                TimeRemainingUpdated?.Invoke(sec == 0 ? new TimeSpan() : new TimeSpan((long)((1 - b.Item2) / (b.Item2 - a.Item2) * sec * 1000 * 1000 * 10)));
            }
            public TimeRemainingMaintainer() : base(10) { }
        }
        protected class SpeedMaintainer : ProgressHistoryMaintainer
        {
            public event Libraries.Events.MyEventHandler<double> SpeedUpdated;
            protected override void Update()
            {
                var ep = GetEndpoints();
                var a = ep.Item1;
                var b = ep.Item2;
                var sec = (b.Item1 - a.Item1).TotalSeconds;
                //MyLogger.Debug($"{a.Item2} {b.Item2}");
                SpeedUpdated?.Invoke(sec == 0 ? 0 : (b.Item2 - a.Item2) / sec);
            }
            public SpeedMaintainer() : base(2) { }
        }
        protected abstract class ProgressHistoryMaintainer
        {
            private double ProgressHistoryLifeTime { get; set; }
            protected ProgressHistoryMaintainer(double progressHistoryLifeTime) { ProgressHistoryLifeTime = progressHistoryLifeTime; }
            Queue<Tuple<DateTime, double>> history = new Queue<Tuple<DateTime, double>>();
            int currentTrackId = 0;
            protected Tuple<Tuple<DateTime,double>,Tuple<DateTime,double>>GetEndpoints()
            {
                lock(history)
                {
                    return new Tuple<Tuple<DateTime, double>, Tuple<DateTime, double>>(history.Peek(), history.Last());
                }
            }
            protected abstract void Update();
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
            public void Add(double progress) { this.Add(progress, DateTime.Now); }
            public void Add(double progress, DateTime occurTime)
            {
                lock (history)
                {
                    history.Enqueue(new Tuple<DateTime, double>(occurTime, progress));
                    StartTrack();
                }
            }
        }
    }
}
