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
                public event Libraries.Events.EmptyEventHandler Started,Pausing,Paused,Completed;
                public event Libraries.Events.MyEventHandler<Tuple<long, long>> FileProgressChanged,FolderProgressChanged,SizeProgressChanged;
                public async Task Start()
                {
                    Started?.Invoke();
                    await StartTask();
                }
                public void Pause()
                {
                    Pausing?.Invoke();
                    PauseTask();
                }
                private void SetCalls(out Action pausedCall, out Action<bool> completedCall, out Action<Tuple<long, long>> fileProgressCall, out Action<Tuple<long, long>> folderProgressCall, out Action<Tuple<long, long>> sizeProgressCall)
                {
                    pausedCall = new Action(() => { Paused?.Invoke(); });
                    completedCall = new Action<bool>((success) => { if (success) Completed?.Invoke(); });
                    fileProgressCall = new Action<Tuple<long, long>>((p) => { FileProgressChanged?.Invoke(p); });
                    folderProgressCall = new Action<Tuple<long, long>>((p) => { FolderProgressChanged?.Invoke(p); });
                    sizeProgressCall = new Action<Tuple<long, long>>((p) => { SizeProgressChanged?.Invoke(p); });
                }
                public Subtask(Func<Task> startTask, Action pauseTask, out Action pausedCall, out Action<bool> completedCall, out Action<Tuple<long, long>> fileProgressCall, out Action<Tuple<long, long>> folderProgressCall, out Action<Tuple<long, long>> sizeProgressCall)
                {
                    StartTask = startTask;
                    PauseTask = pauseTask;
                    SetCalls(out pausedCall, out completedCall, out fileProgressCall, out folderProgressCall, out sizeProgressCall);
                }
            }
        }
    }
}