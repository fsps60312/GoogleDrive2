using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDrive2.Libraries
{
    public interface MyQueuedTask:IComparable
    {
        void SchedulerReleaseSemaphore();
        event Events.MyEventHandler<object> NotifySchedulerCompleted,RemoveFromTaskQueueRequested;
    }
    public class MyTaskQueuePrototype
    {
        protected Libraries.MySet<MyQueuedTask> TaskQueue = new Libraries.MySet<MyQueuedTask>();
        public event Libraries.Events.MyEventHandler<long> QueuedUploaderCountChanged, RunningFileUploadingCountChanged;
        public long MaxConcurrentCount { get; private set; }
        long __RunningCount__ = 0;
        long RunningCount
        {
            get { return __RunningCount__; }
            set
            {
                if (value == __RunningCount__) return;
                __RunningCount__ = value;
                RunningFileUploadingCountChanged?.Invoke(value);
            }
        }
        object syncRoot = new object();
        protected MyTaskQueuePrototype(long maxConcurrentCount)
        {
            MaxConcurrentCount = maxConcurrentCount;
            TaskQueue.CountChanged += (c) => QueuedUploaderCountChanged?.Invoke(c);
        }
        protected bool TrySeekAvailableThread()
        {
            lock (syncRoot)
            {
                if (RunningCount >= MaxConcurrentCount) return false;
                else
                {
                    RunningCount++;
                    return true;
                }
            }
        }
        protected void TaskFinished() { lock (syncRoot) RunningCount--; }
    }
    public class MyTaskQueue : MyTaskQueuePrototype
    {
        Libraries.Events.MyEventHandler<object> notifySchedulerCompletedEventHandler, removeFromTaskQueueRequestedEventHandler;
        public MyTaskQueue(long maxConcurrentCount) : base(maxConcurrentCount)
        {
            notifySchedulerCompletedEventHandler = new Libraries.Events.MyEventHandler<object>((sender) =>
            {
                lock (syncRoot)
                {
                    TaskFinished();
                    UnregisterFileUploader(sender as MyQueuedTask);
                    CheckUploadQueue();
                }
            });
            removeFromTaskQueueRequestedEventHandler = new Events.MyEventHandler<object>((sender) =>
            {
                lock (syncRoot)
                {
                    if (TaskQueue.Remove(sender as MyQueuedTask))
                    {
                        (sender as MyQueuedTask).SchedulerReleaseSemaphore();
                        UnregisterFileUploader(sender as MyQueuedTask);
                    }
                }
            });
        }
        object syncRoot = new object();
        void CheckUploadQueue()
        {
            lock (syncRoot)
            {
                if (TaskQueue.Count > 0)
                {
                    if (TrySeekAvailableThread())
                    {
                        TaskQueue.Dequeue().SchedulerReleaseSemaphore();
                    }
                }
            }
        }
        void RegisterFileUploader(MyQueuedTask task)
        {
            task.NotifySchedulerCompleted += notifySchedulerCompletedEventHandler;
            task.RemoveFromTaskQueueRequested += removeFromTaskQueueRequestedEventHandler;
        }
        void UnregisterFileUploader(MyQueuedTask task)
        {
            task.NotifySchedulerCompleted -= notifySchedulerCompletedEventHandler;
            task.RemoveFromTaskQueueRequested -= removeFromTaskQueueRequestedEventHandler;
        }
        public void AddToQueueAndStart(MyQueuedTask task)
        {
            lock (syncRoot)
            {
                RegisterFileUploader(task);
                MyLogger.Assert(TaskQueue.Enqueue(task));
                CheckUploadQueue();
            }
        }
    }
}
