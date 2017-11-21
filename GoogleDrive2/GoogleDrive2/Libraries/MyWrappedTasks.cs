using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Libraries
{
    abstract class MyWrappedTasks:MyTask
    {
        public event Libraries.Events.MyEventHandler<object> ExtraThreadWaited, ExtraThreadReleased;
        //protected event Libraries.Events.MyEventHandler<object> SubtaskStarted, SubtaskUnstarted;
        List<MyTask> subtasks = new List<MyTask>();
        long subtasksRunningCount = 0, subtasksCompletedCount = 0;
        event Libraries.Events.EmptyEventHandler SemaphoreReleased;
        void DecreaseRunningCount()
        {
            lock (syncRootChangeRunningState)
            {
                if(--subtasksRunningCount==0)
                {
                    SemaphoreReleased?.Invoke();
                }
            }
        }
        MySemaphore GetSemaphore()
        {
            lock(syncRootChangeRunningState)
            {
                var ans = new MySemaphore(0);
                Events.EmptyEventHandler semaphoreReleasedEventHandler = null;
                semaphoreReleasedEventHandler = new Events.EmptyEventHandler(() =>
                {
                    ans.Release();
                    this.SemaphoreReleased -= semaphoreReleasedEventHandler;
                });
                this.SemaphoreReleased += semaphoreReleasedEventHandler;
                return ans;
            }
        }
        void IncreaseRunningCount()
        {
            lock (syncRootChangeRunningState) subtasksRunningCount++;
        }
        async void StartSubtask(MyTask subtask)
        {
            await subtask.StartAsync();
        }
        void StartSubtasks()
        {
            lock (syncRootChangeRunningState)
            {
                IncreaseRunningCount();
                foreach (var task in subtasks) StartSubtask(task);
            }
        }
        protected MyWrappedTasks() : base(false)
        {
            this.Started += delegate { Debug($"{Constants.Icons.Info} Started"); };
            this.Pausing += (sender) =>
            {
                Debug($"{Constants.Icons.Hourglass} Pausing...");
                lock (syncRootChangeRunningState)
                {
                    foreach (var task in subtasks) task.Pause();
                }
            };
        }
        protected void AddSubTask(MyTask subtask)
        {
            lock (syncRootChangeRunningState)
            {
                MyLogger.Assert(!subtask.IsCompleted);
                subtask.Completed += delegate
                {
                    lock (syncRootChangeRunningState) ++subtasksCompletedCount;
                };
                subtask.Started += delegate { IncreaseRunningCount(); };
                subtask.Unstarted += delegate { DecreaseRunningCount(); };
                subtasks.Add(subtask);
                if (!IsPausing) StartSubtask(subtask);
            }
        }
        protected abstract Task<bool> AddSubtasksIfNot();
        public override void CancelPauseRequests()
        {
            lock (syncRootChangeRunningState)
            {
                foreach (var task in subtasks) task.CancelPauseRequests();
                base.CancelPauseRequests();
            }
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
            this.Debug("+++++");
            StartSubtasks();
            await semaphoreAddSubtasks.WaitAsync();
            var allSubtaskAdded = await AddSubtasksIfNot();
            semaphoreAddSubtasks.Release();
            bool isExtraThread = false;
            lock (syncRootChangeRunningState)
            {
                DecreaseRunningCount();
                if (threadCount++ > 0) isExtraThread = true;
                if (isExtraThread) ExtraThreadWaited?.Invoke(this);
            }
            await GetSemaphore().WaitAsync();// Paused might be misjudged if not all thread wait here
            lock (syncRootChangeRunningState)
            {
                if (isExtraThread) ExtraThreadReleased?.Invoke(this);
                threadCount--;
                if (allSubtaskAdded && subtasksCompletedCount == subtasks.Count&&!IsCompleted) OnCompleted();
            }
            this.Debug("-----");
        }
    }
}
