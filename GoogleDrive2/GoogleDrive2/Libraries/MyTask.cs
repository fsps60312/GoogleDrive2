using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Libraries
{
    static class MyTask
    {
        //const int MaxConcurrentTasks = 10;
        //static Libraries.MySemaphore semaphoreSlim = new MySemaphore(MaxConcurrentTasks);
        public static async Task WhenAll(params Task[] tasks) { await WhenAll(tasks as IEnumerable<Task>); }
        public static async Task WhenAll(IEnumerable<Task> tasks)
        {
            await Task.WhenAll(tasks);
            //await Task.Run(() => Parallel.ForEach(tasks, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (task) =>
            //    {
            //        Semaphore semaphore = new Semaphore(0, 1);
            //        new Action(async () => { await task; semaphore.Release(); })();
            //        semaphore.WaitOne();
            //    }));
        }
        public static async Task<T[]>WhenAll<T>(params Task<T>[] tasks) { return await WhenAll(tasks as IEnumerable<Task<T>>); }
        public static async Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks)
        {
            return await Task.WhenAll(tasks);
            //var answer = new T[tasks.ToArray().Length];
            //await Task.Run(() => Parallel.ForEach(tasks, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (task, state, i) =>
            // {
            //     Semaphore semaphore = new Semaphore(0, 1);
            //     new Action(async () => { answer[i] = await task; semaphore.Release(); })();
            //     semaphore.WaitOne();
            // }));
            //return answer;
        }
    }
}
