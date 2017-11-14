using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDrive2.Libraries
{
    partial class MySemaphore
    {
        public static MySemaphore IOsemaphore = new MySemaphore(Constants.MaxConcurrentIOoperations);
    }
    partial class MySemaphore
    {
        SemaphoreSlim mainSemaphore;
        MySemaphore parentSemaphore;
        private int threadCountLimitRequest;
        public int ThreadCountLimitRequest { get { return threadCountLimitRequest; } }
        public MySemaphore(int initialCount, MySemaphore _parentSemaphore = null)
        {
            mainSemaphore = new SemaphoreSlim(0);
            parentSemaphore = _parentSemaphore;
            SetThreadLimit(initialCount);
        }
        public async Task WaitAsync()
        {
            await mainSemaphore.WaitAsync();
            if (parentSemaphore != null) await parentSemaphore.WaitAsync();
        }
        public void Release()
        {
            parentSemaphore?.Release();
            lock (mainSemaphore)
            {
                mainSemaphore.Release();
            }
        }
        public async void SetThreadLimit(int limit)
        {
            var origin = Interlocked.Exchange(ref threadCountLimitRequest, limit);
            for (; origin < limit; origin++) this.Release();
            for (; origin > limit; origin--) await this.WaitAsync();
        }
    }
}
