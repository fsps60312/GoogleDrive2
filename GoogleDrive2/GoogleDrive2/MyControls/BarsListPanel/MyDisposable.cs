using System;
using System.ComponentModel;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public abstract class MyDisposable:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event Libraries.Events.MyEventHandler<object> Disposed;
        public Func<System.Threading.Tasks.Task>Disposing = null;
        public void UnregisterDisposingEvents() { Disposing = null; }
        public event Libraries.Events.MyEventHandler<double> HeightChanged;
        public void OnHeightChanged(double difference)
        {
            HeightChanged?.Invoke(difference);
        }
        public async System.Threading.Tasks.Task OnDisposed(bool animated=true)
        {
            if (animated && Disposing != null) await Disposing();
            Disposed?.Invoke(this);
            Disposing = null; Disposed = null;
        }
    }
}
