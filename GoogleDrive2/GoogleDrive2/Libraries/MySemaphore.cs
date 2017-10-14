using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDrive2.Libraries
{
    class MySemaphore
    {
        SemaphoreSlim mainSemaphore, setSemaphore;
        MySemaphore parentSemaphore;
        private int threadCountLimit,threadCountLimitRequest;
        public int ThreadCountLimitRequest { get { return threadCountLimitRequest; } }
        public MySemaphore(int initialCount, MySemaphore _parentSemaphore = null)
        {
            mainSemaphore = new SemaphoreSlim(0);
            parentSemaphore = _parentSemaphore;
            setSemaphore = new SemaphoreSlim(1);
            threadCountLimit = threadCountLimitRequest = 0;
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
        public void SetThreadLimit(int limit)
        {
            threadCountLimitRequest = limit;
            new Action(async () =>
            {
                await setSemaphore.WaitAsync();
                try
                {
                    for (; threadCountLimit < threadCountLimitRequest; threadCountLimit++)
                    {
                        this.Release();
                    }
                    for (; threadCountLimit > threadCountLimitRequest; threadCountLimit--)
                    {
                        await mainSemaphore.WaitAsync();
                    }
                }
                finally
                {
                    setSemaphore.Release();
                }
            })();
        }
    }
}
