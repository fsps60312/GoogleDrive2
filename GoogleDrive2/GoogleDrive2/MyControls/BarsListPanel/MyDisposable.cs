using System;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public abstract class MyDisposable
    {
        public delegate void MyDisposableEventHandler();
        public event MyDisposableEventHandler Disposed;
        public Func<System.Threading.Tasks.Task>Disposing = null;
        public void UnregisterDisposingEvents() { Disposing = null; }
        public delegate void HeightChangedEventHandler(double difference);
        public event HeightChangedEventHandler HeightChanged;
        public void OnHeightChanged(double difference)
        {
            HeightChanged?.Invoke(difference);
        }
        public async System.Threading.Tasks.Task OnDisposed(bool animated=true)
        {
            if (animated && Disposing != null) await Disposing();
            Disposed?.Invoke();
            Disposing = null; Disposed = null;
        }
    }
}
