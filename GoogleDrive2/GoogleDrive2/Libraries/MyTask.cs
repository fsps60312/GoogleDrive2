using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using GoogleDrive2.Libraries.Events;

namespace GoogleDrive2.Libraries
{
    public abstract partial class MyTask : MyLoggerClass, MyQueuedTask
    {
        protected object syncRootChangeRunningState = new object();
        public event MyEventHandler<object> Started, Unstarted, Pausing,Completed;
        public event Libraries.Events.MyEventHandler<string> MessageAppended;
        protected void OnMessageAppended(string msg) { MessageAppended?.Invoke(msg); }
        protected void OnCompleted()
        {
            lock (syncRootChangeRunningState)
            {
                if (IsCompleted)
                {
                    this.LogError("OnCompleted() twice or more");
                    return;
                }
                IsCompleted = true;
                Completed?.Invoke(this);
            }
        }
        public event MyEventHandler<object> NotifySchedulerCompleted;
        public event MyEventHandler<object> RemoveFromTaskQueueRequested;
        protected event MyEventHandler<object> Queued, Unqueued;
        Libraries.MySemaphore semaphore = new MySemaphore(0), semaphoreStartAsync = null;
        public void SchedulerReleaseSemaphore() { semaphore.Release(); }
        public int CompareTo(object obj)
        {
            return SerialNumber.CompareTo((obj as MyTask).SerialNumber);
        }
        static long SerialNumberCounter = 0;
        long SerialNumber;
        protected MyTask(bool preventStartTwice = true)
        {
            if (preventStartTwice) semaphoreStartAsync = new MySemaphore(1);
            SerialNumber = Interlocked.Increment(ref SerialNumberCounter);
            this.ErrorLogged += (error) => OnMessageAppended($"{Constants.Icons.Warning} {error}");
            this.Debugged += (msg) => OnMessageAppended($"{msg}");
        }
        public void Pause()
        {
            lock (syncRootChangeRunningState)
            {
                if (!IsRunning || IsPausing) return;
                IsPausing = true;
                Pausing?.Invoke(this);
                RemoveFromTaskQueueRequested?.Invoke(this);
            }
        }
        static MyTaskQueue unlimitedTaskQueue = new MyTaskQueue(long.MaxValue);
        protected MyTaskQueue TaskQueue { get; set; } = unlimitedTaskQueue;
        public bool IsPausing { get; protected set; } = false;
        public bool IsRunning { get; protected set; } = false;
        public bool IsCompleted { get; protected set; } = false;
        public virtual void CancelPauseRequests()
        {
            lock (syncRootChangeRunningState)
            {
                Debug("Canceling pause requests...");
                if (IsPausing)
                {
                    IsPausing = false;
                    Debug("Pause request Canceled");
                }
            }
        }
        protected abstract Task PrepareBeforeStartAsync();
        protected abstract Task StartMainTaskAsync();
        public async Task StartAsync()
        {
            CancelPauseRequests();
            if (semaphoreStartAsync != null) await semaphoreStartAsync.WaitAsync();
            try
            {
                lock (syncRootChangeRunningState)
                {
                    if (IsCompleted || IsPausing) return;//must be the first
                    IsRunning = true;
                    Started?.Invoke(this);
                }
                try
                {
                    await PrepareBeforeStartAsync();
                    TaskQueue.AddToQueueAndStart(this);
                    Queued?.Invoke(this);
                    await semaphore.WaitAsync();
                    Unqueued?.Invoke(this);
                    await StartMainTaskAsync();//OnCompleted() might be called here
                                               //Now IsCompleted is determined
                }
                finally
                {
                    lock (syncRootChangeRunningState)
                    {
                        //IsPausing = false;
                        IsRunning = false;
                        NotifySchedulerCompleted?.Invoke(this);
                        Unstarted?.Invoke(this);
                    }
                }
            }
            finally { if (semaphoreStartAsync != null) semaphoreStartAsync.Release(); }
        }
    }
}
