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
        object syncRoot = new object();
        List<MyTask> subtasks = new List<MyTask>();
        bool isSubtasksPaused = true;
        long subtasksRunningCount = 0;
        Libraries.MySemaphore semaphore = new MySemaphore(0);
        void DecreaseRunningCount()
        {
            lock(syncRoot)
            {
                if(--subtasksRunningCount==0)
                {
                    semaphore.Release();
                    isSubtasksPaused = true;
                }
            }
        }
        void IncreaseRunningCount()
        {
            lock (syncRoot) subtasksRunningCount++;
        }
        async void StartSubask(MyTask subtask)
        {
            IncreaseRunningCount();
            try { await subtask.StartAsync(); }
            finally { DecreaseRunningCount(); }
        }
        protected MyWrappedTasks()
        {
            this.Pausing += (sender) =>
            {
                lock (syncRoot)
                {
                    foreach (var task in subtasks) task.Pause();
                    isSubtasksPaused = true;
                }
            };
        }
        protected void AddSubTask(MyTask subtask)
        {
            lock (syncRoot)
            {
                subtasks.Add(subtask);
                if (!isSubtasksPaused) StartSubask(subtask);
            }
        }
        protected abstract Task AddSubtasksIfNot();
        protected override async Task StartMainTaskAsync()
        {
            lock (syncRoot)
            {
                IncreaseRunningCount();
                foreach (var task in subtasks) StartSubask(task);
                isSubtasksPaused = false;
            }
            await AddSubtasksIfNot();
            DecreaseRunningCount();
            await semaphore.WaitAsync();
        }
        protected override Task PrepareBeforeStartAsync()
        {
            //do nothing
            return Task.CompletedTask;
        }
    }
}
