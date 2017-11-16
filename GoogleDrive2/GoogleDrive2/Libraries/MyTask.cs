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
        public event MyEventHandler<object> Started, Unstarted, Pausing,Completed;
        public event Libraries.Events.MyEventHandler<string> MessageAppended;
        protected void OnMessageAppended(string msg) { MessageAppended?.Invoke(msg); }
        protected void OnCompleted() { IsCompleted = true; Completed?.Invoke(this); }
        public event MyEventHandler<object> NotifySchedulerCompleted;
        public event MyEventHandler<object> RemoveFromTaskQueueRequested;
        protected event MyEventHandler<object> Queued, Unqueued;
        Libraries.MySemaphore semaphore = new MySemaphore(0), semaphoreStartAsync = new MySemaphore(1);
        public void SchedulerReleaseSemaphore() { semaphore.Release(); }
        public int CompareTo(object obj)
        {
            return SerialNumber.CompareTo((obj as MyTask).SerialNumber);
        }
        static long SerialNumberCounter = 0;
        long SerialNumber;
        protected MyTask()
        {
            SerialNumber = Interlocked.Increment(ref SerialNumberCounter);
            this.ErrorLogged += (error) => OnMessageAppended($"{Constants.Icons.Warning} {error}");
            this.Debugged += (msg) => OnMessageAppended($"{msg}");
        }
        public void Pause()
        {
            IsPausing = true;
            Pausing?.Invoke(this);
            RemoveFromTaskQueueRequested?.Invoke(this);
        }
        protected abstract Task PrepareBeforeStartAsync();
        static MyTaskQueue unlimitedTaskQueue = new MyTaskQueue(long.MaxValue);
        protected MyTaskQueue TaskQueue { get; set; } = unlimitedTaskQueue;
        public bool IsPausing { get; protected set; } = false;
        public bool IsRunning { get; protected set; } = false;
        public bool IsCompleted { get; protected set; } = false;
        protected abstract Task StartMainTaskAsync();
        public async Task StartAsync()
        {
            IsPausing = false;
            await semaphoreStartAsync.WaitAsync();
            if (IsCompleted) return;
            IsRunning = true;
            Started?.Invoke(this);
            try
            {
                await PrepareBeforeStartAsync();
                TaskQueue.AddToQueueAndStart(this);
                Queued?.Invoke(this);
                await semaphore.WaitAsync();
                Unqueued?.Invoke(this);
                await StartMainTaskAsync();
            }
            finally
            {
                IsPausing = false;
                IsRunning = false;
                Unstarted?.Invoke(this);
                NotifySchedulerCompleted?.Invoke(this);
                semaphoreStartAsync.Release();
            }
        }
    }
}
