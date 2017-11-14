using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public abstract class MyDisposable:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        const double MinUpdatePeriodForEachProperty = 0.5;
        Dictionary<string, DateTime> nextPropertyChangeTime = new Dictionary<string, DateTime>();
        Dictionary<string, int> propertyChangeRequestId = new Dictionary<string, int>();
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //int id;
            //lock (propertyChangeRequestId)
            //{
            //    if (!propertyChangeRequestId.ContainsKey(propertyName)) propertyChangeRequestId.Add(propertyName, 0);
            //    id = ++propertyChangeRequestId[propertyName];
            //}
            //lock (nextPropertyChangeTime)
            //{
            //    if (!nextPropertyChangeTime.ContainsKey(propertyName)) nextPropertyChangeTime.Add(propertyName, DateTime.MinValue);
            //    var timeNext = nextPropertyChangeTime[propertyName];
            //    var timeNow = DateTime.Now;
            //    if (timeNow >= timeNext)
            //    {
            //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //        nextPropertyChangeTime[propertyName] = timeNow.AddSeconds(MinUpdatePeriodForEachProperty);
            //    }
            //    else
            //    {
            //        var timeToWait = (int)((timeNext - timeNow).TotalMilliseconds) + 1;
            //        MyLogger.Debug($"timeToWait={timeToWait}");
            //        new Action(async () =>
            //        {
            //            await System.Threading.Tasks.Task.Delay(timeToWait);
            //            lock(propertyChangeRequestId)
            //            {
            //                if (propertyChangeRequestId[propertyName] == id)
            //                {
            //                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //                    lock (nextPropertyChangeTime)
            //                    {
            //                        nextPropertyChangeTime[propertyName] = DateTime.Now.AddSeconds(MinUpdatePeriodForEachProperty);
            //                    }
            //                }
            //                else { MyLogger.Debug("obleted"); }
            //            }
            //        })();
            //    }
            //}
        }
        public event Libraries.Events.MyEventHandler<object> Disposed;
        public Func<System.Threading.Tasks.Task>Disposing = null;
        public void UnregisterDisposingEvents() { Disposing = null; }
        public event Libraries.Events.MyEventHandler<double> HeightChanged;
        public void OnHeightChanged(double difference)
        {
            HeightChanged?.Invoke(difference);
        }
        public void OnDisposed()
        {
            Disposed?.Invoke(this);
            Disposing = null; Disposed = null;
        }
        public async System.Threading.Tasks.Task OnDisposedAsync(bool animated=true)
        {
            if (animated && Disposing != null) await Disposing();
            OnDisposed();
        }
    }
}
