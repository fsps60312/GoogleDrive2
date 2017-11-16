using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GoogleDrive2.Libraries
{
    class MySet<T>
    {
        object syncRoot = new object();
        SortedSet<T> queue = new SortedSet<T>();
        public event Libraries.Events.MyEventHandler<int> CountChanged;
        public void SetComparer(IComparer<T> comparer)
        {
            lock (syncRoot)
            {
                var preQueue = queue;
                queue = new SortedSet<T>(preQueue, comparer);
            }
        }
        public T Dequeue()
        {
            lock (syncRoot)
            {
                var answer = queue.ElementAt(0);
                MyLogger.Assert(queue.Remove(answer));
                CountChanged?.Invoke(Count);
                return answer;
            }
        }
        public bool Enqueue(T v)
        {
            lock (syncRoot)
            {
                var answer = queue.Add(v);
                CountChanged?.Invoke(Count);
                return answer;
            }
        }
        public int Count { get { lock (syncRoot) return queue.Count; } }
        public bool Remove(T v)
        {
            lock (syncRoot)
            {
                var answer = queue.Remove(v);
                CountChanged?.Invoke(Count);
                return answer;
            }
        }
    }
}
