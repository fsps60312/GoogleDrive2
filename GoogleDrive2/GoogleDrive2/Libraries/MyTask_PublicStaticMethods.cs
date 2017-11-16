using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.Libraries
{
    partial class MyTask
    {
        public static async Task WhenAll(params Task[] tasks) { await WhenAll(tasks as IEnumerable<Task>); }
        public static async Task WhenAll(IEnumerable<Task> tasks)
        {
            await Task.WhenAll(tasks);
        }
        public static async Task<T[]>WhenAll<T>(params Task<T>[] tasks) { return await WhenAll(tasks as IEnumerable<Task<T>>); }
        public static async Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks)
        {
            return await Task.WhenAll(tasks);
        }
    }
}
