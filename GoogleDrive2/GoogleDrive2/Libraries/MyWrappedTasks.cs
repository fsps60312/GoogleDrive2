using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace GoogleDrive2.Libraries
{
    abstract class MyWrappedTasks : MyTask
    {
        public event Libraries.Events.MyEventHandler<object> ExtraThreadWaited, ExtraThreadReleased;
        //protected event Libraries.Events.MyEventHandler<object> SubtaskStarted, SubtaskUnstarted;
        private object
            syncRootSubtasks = new object(),
            syncRootSemaphoreReleased = new object();
        List<MyTask> subtasks = new List<MyTask>();
        long subtasksRunningCount = 0, subtasksCompletedCount = 0;
        event Libraries.Events.EmptyEventHandler SemaphoreReleased;
        void DecreaseRunningCount()
        {
            if (Interlocked.Decrement(ref subtasksRunningCount) == 0)
            {
                lock (syncRootSemaphoreReleased) SemaphoreReleased?.Invoke();
            }
        }
        MySemaphore GetSemaphore()
        {
            var ans = new MySemaphore(0);
            Events.EmptyEventHandler semaphoreReleasedEventHandler = null;
            semaphoreReleasedEventHandler = new Events.EmptyEventHandler(() =>
            {
                lock (syncRootSemaphoreReleased)
                {
                    ans.Release();
                    this.SemaphoreReleased -= semaphoreReleasedEventHandler;
                }
            });
            lock (syncRootSemaphoreReleased) this.SemaphoreReleased += semaphoreReleasedEventHandler;
            return ans;
        }
        void IncreaseRunningCount()
        {
            Interlocked.Increment(ref subtasksRunningCount);
        }
        async void StartSubtask(MyTask subtask)
        {
            IncreaseRunningCount();
            await subtask.StartAsync();
            DecreaseRunningCount();
        }
        void StartSubtasks()
        {
            IncreaseRunningCount();
            lock (syncRootSubtasks)
            {
                foreach (var task in subtasks) StartSubtask(task);
            }
        }
        protected MyWrappedTasks() : base(false)
        {
            this.Started += delegate { Debug($"{Constants.Icons.Info} Started"); };
            this.Pausing += (sender) =>
            {
                Debug($"{Constants.Icons.Hourglass} Pausing...");
                lock (syncRootSubtasks)
                {
                    foreach (var task in subtasks) task.Pause();
                }
            };
        }
        protected void AddSubTask(MyTask subtask)
        {
            MyLogger.Assert(!subtask.IsCompleted);
            subtask.Completed += delegate
            {
                Interlocked.Increment(ref subtasksCompletedCount);
            };
            //subtask.Started += delegate { IncreaseRunningCount(); };
            //subtask.Unstarted += delegate { DecreaseRunningCount(); };
            lock (syncRootSubtasks) subtasks.Add(subtask);
            if (IsRunningRequest) StartSubtask(subtask);
        }
        protected abstract Task<bool> AddSubtasksIfNot();
        public override void CancelPauseRequests()
        {
            lock (syncRootSubtasks)
            {
                foreach (var task in subtasks) task.CancelPauseRequests();
            }
            base.CancelPauseRequests();
        }
        protected override Task PrepareBeforeStartAsync()
        {
            //do nothing
            return Task.CompletedTask;
        }
        Libraries.MySemaphore semaphoreAddSubtasks = new MySemaphore(1);
        int threadCount = 0;
        protected override async Task StartMainTaskAsync()
        {
            StartSubtasks();
            await semaphoreAddSubtasks.WaitAsync();
            var allSubtaskAdded = await AddSubtasksIfNot();
            semaphoreAddSubtasks.Release();
            Libraries.MySemaphore semaphore = GetSemaphore();
            DecreaseRunningCount();
            if (Interlocked.Increment(ref threadCount) > 1) ExtraThreadWaited?.Invoke(this);
            await semaphore.WaitAsync();// Paused might be misjudged if not all thread wait here
            if (Interlocked.Decrement(ref threadCount) > 0) ExtraThreadReleased?.Invoke(this);
            bool allCompleted;
            lock (syncRootSubtasks) allCompleted = (Interlocked.Read(ref subtasksCompletedCount) == subtasks.Count);
            if (allSubtaskAdded && allCompleted && !IsCompleted) OnCompleted();
        }
    }
}
