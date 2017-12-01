using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public abstract class MyDisposable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        const double MinUpdatePeriodForEachProperty = 0.5;
        Dictionary<string, DateTime> nextPropertyChangeTime = new Dictionary<string, DateTime>();
        Dictionary<string, int> propertyChangeRequestId = new Dictionary<string, int>();
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event Libraries.Events.MyEventHandler<object> Disposed;
        public Func<System.Threading.Tasks.Task> Disposing = null;
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
        public async System.Threading.Tasks.Task OnDisposedAsync(bool animated = true)
        {
            if (animated && Disposing != null) await Disposing();
            OnDisposed();
        }
    }
}
