using AuremCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class PerPidWorkerPool : IWorkerPool
    {
        private ushort NProc;
        private int Multiple;
        private Func<ushort, Task> Work;
        private WaitGroup Wg;
        private ulong Quit;

        public PerPidWorkerPool(ushort nproc, int multiple, Func<ushort, Task> work)
        {
            Wg = new WaitGroup();
            Quit = 0;
            NProc = nproc;
            Multiple = multiple;
            Work = work;
        }

        public void Start()
        {
            Wg.Add(Multiple * NProc);
            for (ushort i = 0; i < NProc; i++)
            {
                for (int j = 0; j < Multiple; j++)
                {
                    var xi = i;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            while (Interlocked.Read(ref Quit) == 0)
                            {
                                await Work(xi);
                            }
                        }
                        finally
                        {
                            Wg.Done();
                        }
                    });
                }
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
