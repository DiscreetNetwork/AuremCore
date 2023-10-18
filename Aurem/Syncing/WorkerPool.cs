using AuremCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    internal class WorkerPool : IWorkerPool
    {
        private int Size;
        private ulong Quit;
        private WaitGroup Wg;
        private Func<Task> Work;

        public WorkerPool(int sz, Func<Task> work)
        {
            Quit = 0;
            Wg = new WaitGroup();
            Size = sz;
            Work = work;
        }

        public void Start()
        {
            Wg.Add(Size);
            for (int i = 0; i < Size; i++)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (Interlocked.Read(ref Quit) == 0)
                        {
                            await Work();
                            await Task.Delay(10);
                        }
                    }
                    finally
                    {
                        Wg.Done();
                    }
                });
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref Quit, 1);
            Wg.Wait();
        }

        public Task StopAsync()
        {
            Interlocked.Exchange(ref Quit, 1);
            return Wg.WaitAsync();
        }
    }
}
