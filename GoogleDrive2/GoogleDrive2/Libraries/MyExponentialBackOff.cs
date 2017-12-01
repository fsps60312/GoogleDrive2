using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive2.Libraries
{
    static class MyExponentialBackOff
    {
        const int MaxTimeToWait = 500 * 16;
        public static async Task<bool> Do(Func<Task<bool>> task, Func<int, Task> whenRetrying = null)
        {
            for (int timeToWait = 500; !await task();)
            {
                if (timeToWait > MaxTimeToWait) return false;
                if (whenRetrying != null)
                {
                    await whenRetrying(timeToWait);
                }
                await Task.Delay(timeToWait);
                timeToWait *= 2;
            }
            return true;
        }
    }
}
