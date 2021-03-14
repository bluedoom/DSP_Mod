using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AsyncUpdate
{
    public static class MTInfo
    {
        public readonly static int ProcessorCount;
        static MTInfo(){
            Console.WriteLine($"Environment.ProcessorCount{Environment.ProcessorCount}");
            ProcessorCount = Environment.ProcessorCount;
            ThreadPool.GetAvailableThreads(out var workerThreads, out var iot);
            ThreadPool.SetMinThreads(Math.Max(ProcessorCount,workerThreads), iot);
        }
    }


    public abstract class MTRunner<T>
    {
        protected T[] items;
        public virtual int TotalCount { get; set; }
        int current;
        int currentThreadCount;
        Semaphore waiter = new Semaphore(0,1);
        readonly WaitCallback callback;
        public MTRunner()
        {
            callback = DoParallel;
        }

        void DoParallel(object _)
        {
            int i;
            try
            {
                while ((i = Interlocked.Increment(ref current))< TotalCount)
                {
                    Process(i);
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

        public abstract void Process(int i);

        public void DoParallel(T[] items,int start, int count)
        {
            TotalCount = count;
            this.items = items;
            current = start - 1;
            currentThreadCount = MTInfo.ProcessorCount;
            for (int i = 0; i < MTInfo.ProcessorCount; i++)
            {
                ThreadPool.QueueUserWorkItem(callback);
            }
            
            waiter.WaitOne();

            this.items = null;
        }
    }
}
