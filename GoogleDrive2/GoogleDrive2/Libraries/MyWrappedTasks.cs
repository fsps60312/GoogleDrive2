using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Libraries
{
    abstract class MyWrappedTasks:MyTask
    {
        //protected event Libraries.Events.MyEventHandler<object> SubtaskStarted, SubtaskUnstarted;
        List<MyTask> subtasks = new List<MyTask>();
        long subtasksRunningCount = 0, subtasksCompletedCount = 0;
        Libraries.MySemaphore semaphore = new MySemaphore(0);
        void DecreaseRunningCount()
        {
            lock (syncRootChangeRunningState)
            {
                if(--subtasksRunningCount==0)
                {
                    semaphore.Release();
                }
            }
        }
        void IncreaseRunningCount()
        {
            lock (syncRootChangeRunningState) subtasksRunningCount++;
        }
        async void StartSubask(MyTask subtask)
        {
            await subtask.StartAsync();
        }
        protected MyWrappedTasks()
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
                if (!IsPausing) StartSubask(subtask);
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
        protected override void ReturnedBeforeStartMainTask()
        {
            lock (syncRootChangeRunningState)
            {
                base.ReturnedBeforeStartMainTask();
            }
        }
        protected override Task PrepareBeforeStartAsync()
        {
            //do nothing
            return Task.CompletedTask;
        }
        protected override async Task StartMainTaskAsync()
        {
            lock (syncRootChangeRunningState)
            {
                IncreaseRunningCount();
                foreach (var task in subtasks) StartSubask(task);
            }
            var allSubtaskAdded = await AddSubtasksIfNot();
            lock (syncRootChangeRunningState)
            {
                DecreaseRunningCount();
            }
            await semaphore.WaitAsync();
            lock (syncRootChangeRunningState)
            {
                if (allSubtaskAdded && subtasksCompletedCount == subtasks.Count) OnCompleted();
            }
        }
    }
}
