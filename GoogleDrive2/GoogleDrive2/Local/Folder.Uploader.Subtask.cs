﻿using System.Threading.Tasks;
using System;

namespace GoogleDrive2.Local
{
    partial class Folder
    {
        public partial class Uploader
        {
            class Subtask
            {
                private Func<Task> StartTask;
                private Action PauseTask;
                public event Libraries.Events.MyEventHandler<Tuple<long, long>> FileProgressChanged, FolderProgressChanged, SizeProgressChanged,
                    LocalSearchStatusChanged, RunningTaskCountChanged;
                bool IsCompleted = false, IsRunning = false;
                Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
                public void OnStarted() { IsRunning = true; }
                public void OnCompleted(bool success)
                {
                    IsRunning = false;
                    IsCompleted = success;
                }
                public async Task Start()
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        if (IsCompleted) return;
                        await StartTask();
                    }
                    finally { semaphore.Release(); }
                }
                public void Pause()
                {
                    if (IsRunning) PauseTask();
                }
                private void SetCalls(
                    out Action<Tuple<long, long>> fileProgressCall,
                    out Action<Tuple<long, long>> folderProgressCall,
                    out Action<Tuple<long, long>> sizeProgressCall,
                    out Action<Tuple<long, long>> localSearchStatusCall,
                    out Action<Tuple<long, long>> runningTaskCountCall)
                {
                    fileProgressCall = new Action<Tuple<long, long>>((p) => { FileProgressChanged?.Invoke(p); });
                    folderProgressCall = new Action<Tuple<long, long>>((p) => { FolderProgressChanged?.Invoke(p); });
                    sizeProgressCall = new Action<Tuple<long, long>>((p) => { SizeProgressChanged?.Invoke(p); });
                    localSearchStatusCall = new Action<Tuple<long, long>>((p) => { LocalSearchStatusChanged?.Invoke(p); });
                    runningTaskCountCall = new Action<Tuple<long, long>>((p) => { RunningTaskCountChanged?.Invoke(p); });
                }
                public Subtask(
                    Func<Task> startTask, Action pauseTask,
                    out Action<Tuple<long, long>> fileProgressCall,
                    out Action<Tuple<long, long>> folderProgressCall,
                    out Action<Tuple<long, long>> sizeProgressCall,
                    out Action<Tuple<long, long>> localSearchStatusCall,
                    out Action<Tuple<long, long>> runningTaskCountCall)
                {
                    StartTask = startTask;
                    PauseTask = pauseTask;
                    SetCalls(out fileProgressCall, out folderProgressCall, out sizeProgressCall, out localSearchStatusCall, out runningTaskCountCall);
                }
            }
        }
    }
}
