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
            const double RemainingTimeTolerance = 0.2;
            public event Libraries.Events.MyEventHandler<TimeSpan> TimeRemainingUpdated;
            //int currentUpdateId = 0;
            //private TimeSpan previousCountDownData = TimeSpan.MaxValue;
            //private async void StartCountDown(TimeSpan countDownData)
            //{
            //    int id = System.Threading.Interlocked.Increment(ref currentUpdateId);
            //    if (countDownData == TimeSpan.Zero)
            //    {
            //        await Task.Delay(100);
            //        TimeRemainingUpdated?.Invoke(countDownData);
            //        return;
            //    }
            //    var preCountDownData = previousCountDownData == TimeSpan.MaxValue ? countDownData : previousCountDownData;
            //    previousCountDownData = countDownData;
            //    DateTime
            //        timeNow = DateTime.Now,
            //        timeMid = timeNow.Add(new TimeSpan(Math.Min(countDownData.Ticks, 10 * (long)1e7))),
            //        timeEnd = timeNow.Add(countDownData),
            //        timeBegin = timeEnd.Add(preCountDownData.Negate());

            //    var ratio = (double)(timeMid - timeBegin).Ticks / (timeMid - timeNow).Ticks;
            //    //if (ratio < 0.5) ratio = 0.5;
            //    //if (ratio > 2) ratio = 2;
            //    var getRemainingTime = new Func<Tuple<double, TimeSpan>>(() =>
            //       {
            //           var t = DateTime.Now;
            //           if (t >= timeMid) return new Tuple<double, TimeSpan>(1, (timeEnd - t));
            //           else
            //           {
            //               var ticks = (long)((timeMid - t).Ticks * ratio);
            //               return new Tuple<double, TimeSpan>(ratio, new TimeSpan(ticks) + (timeEnd - timeMid));
            //           }
            //       });
            //    while (currentUpdateId == id)
            //    {
            //        var timeRemaing = getRemainingTime();
            //        previousCountDownData = timeRemaing.Item2;
            //        TimeRemainingUpdated?.Invoke(timeRemaing.Item2);
            //        var timeToWait = (int)(timeRemaing.Item2.Milliseconds / timeRemaing.Item1) + 1;
            //        await Task.Delay(100);
            //        //await Task.Delay(Math.Max(100, timeToWait));
            //    }
            //}
            DateTime nextCountDownData;
            int runningCountDown = 0;
            private TimeSpan AdjustToFitIntoTolerance(TimeSpan a, TimeSpan b)
            {
                if (a.Ticks * (1 - RemainingTimeTolerance) > b.Ticks) a = new TimeSpan((long)Math.Floor(b.Ticks / (1 - RemainingTimeTolerance)));
                if (b.Ticks * (1 - RemainingTimeTolerance) > a.Ticks) a = new TimeSpan((long)Math.Ceiling(b.Ticks * (1 - RemainingTimeTolerance)));
                return a;
            }
            private async void CountDown(TimeSpan timeRemaining)
            {
                DateTime endTime = nextCountDownData = DateTime.Now.Add(timeRemaining);
                if (System.Threading.Interlocked.CompareExchange(ref runningCountDown, 1, 0) != 0) return;
                try
                {
                    while (true)
                    {
                        var timeNow = DateTime.Now;
                        endTime = timeNow.Add(AdjustToFitIntoTolerance(endTime - timeNow, nextCountDownData - timeNow));
                        if (timeNow >= endTime)
                        {
                            TimeRemainingUpdated?.Invoke(TimeSpan.Zero);
                            break;
                        }
                        TimeRemainingUpdated?.Invoke(endTime - timeNow);
                        await Task.Delay((endTime - timeNow).Milliseconds);
                    }
                }
                finally { MyLogger.Assert(System.Threading.Interlocked.Exchange(ref runningCountDown, 0) == 1); }
            }
            protected override void Update()
            {
                var ep = GetEndpoints();
                var a = ep.Item1;
                var b = ep.Item2;
                var sec = (b.Item1 - a.Item1).TotalSeconds;
                //MyLogger.Debug($"{a.Item2} {b.Item2}");
                var timeRemaining = sec == 0 ? new TimeSpan() : new TimeSpan((long)((1 - b.Item2) / (b.Item2 - a.Item2) * sec * 1000 * 1000 * 10));
                //TimeRemainingUpdated?.Invoke(timeRemaining);
                if (new TimeSpan(-30, 0, 0, 0) <= timeRemaining && timeRemaining <= new TimeSpan(30, 0, 0, 0))
                {
                    CountDown(timeRemaining);
                }
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
            public SpeedMaintainer() : base(3) { }
        }
        protected abstract class ProgressHistoryMaintainer
        {
            private double ProgressHistoryLifeTime { get; set; }
            protected ProgressHistoryMaintainer(double progressHistoryLifeTime) { ProgressHistoryLifeTime = progressHistoryLifeTime; }
            Queue<Tuple<DateTime, double>> history = new Queue<Tuple<DateTime, double>>();
            int currentTrackId = 0;
            protected Tuple<Tuple<DateTime, double>, Tuple<DateTime, double>> GetEndpoints()
            {
                lock (history)
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
