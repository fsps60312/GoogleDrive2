using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDrive2.Libraries
{
    class FrequentExecutionLimiter
    {
        double MinPeriod;
        DateTime NextExecutionTime = DateTime.MinValue;
        long serialNumber = 0;
        public FrequentExecutionLimiter(double minPeriod) { MinPeriod = minPeriod; }
        public async void Execute(Action action)
        {
            var id = Interlocked.Increment(ref serialNumber);
            var timeNow = DateTime.Now;
            var nextExecutionTime = NextExecutionTime;
            if (timeNow < nextExecutionTime)
            {
                var timeToWait = (int)(nextExecutionTime - timeNow).TotalMilliseconds + 1;
                await Task.Delay(timeToWait);
            }
            if (Interlocked.Read(ref serialNumber) == id)
            {
                action();
                NextExecutionTime = DateTime.Now.AddSeconds(MinPeriod);
            }
        }
    }
}
