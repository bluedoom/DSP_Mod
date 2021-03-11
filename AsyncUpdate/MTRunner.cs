using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AsyncUpdate
{
    public abstract class MTRunner<T>
    {
        T[] items;
        int totalCount;
        int current;
        readonly int totalThreadCount;
        int currentThreadCount;
        Semaphore waiter = new Semaphore(0,1);
        readonly WaitCallback callback;
        public MTRunner(int threadCount = 2)
        {
            totalThreadCount = threadCount;
            callback = DoParallel;
        }

        void DoParallel(object _)
        {
            int i;
            try
            {
                while ((i = Interlocked.Increment(ref current))< totalCount)
                {
                    T item = items[i];
                    Process(in item,i);
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref currentThreadCount) < 1)
                {
                    waiter.Release();
                }
            }
        }

        public abstract void Process(in T item,int i);

        public void DoParallel(T[] items,int start, int count)
        {
            totalCount = count;
            this.items = items;
            current = start - 1;
            currentThreadCount = totalThreadCount;
            for (int i = 0; i < totalThreadCount; i++)
            {
                ThreadPool.QueueUserWorkItem(callback);
            }
            
            waiter.WaitOne();
            this.items = null;
        }
    }
}
