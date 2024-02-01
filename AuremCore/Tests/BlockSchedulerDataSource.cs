using AuremCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public class BlockSchedulerDataSource : IDataSource
    {
        private int size;
        private Random random;
        private TimeSpan interval;
        private int nproc;
        private int pid;
        private int numProduced;
        private DateTime lastProduced;
        private DateTime currentBeginning;
        private DateTime currentEnd;
        private bool sessionBarrier;
        private bool preventData;

        public BlockSchedulerDataSource(int size, TimeSpan interval, int nproc, int pid, bool preventData)
        {
            this.size = size;
            random = new Random();
            this.interval = interval;
            this.pid = pid;
            this.nproc = nproc;
            this.numProduced = 0;
            lastProduced = DateTime.MinValue;
            var ts = DateTime.UtcNow.Ticks;
            currentBeginning = new DateTime(ts - (ts % interval.Ticks));
            currentEnd = currentBeginning.Add(interval);
            sessionBarrier = false;
            this.preventData = preventData;
        }

        public void SetSessionBarrier()
        {
            sessionBarrier = true;
        }

        public async Task<byte[]> Get()
        {
            // 0 goes first, then 1, then 2, etc...
            if (lastProduced == DateTime.MinValue || sessionBarrier)
            {
                // update the beginning
                while (currentEnd < DateTime.UtcNow)
                {
                    currentBeginning += interval;
                    currentEnd += interval;
                }

                if (sessionBarrier) sessionBarrier = false;
            }

            try
            {
                while (DateTime.UtcNow < currentBeginning) await Task.Delay(10);
                if (!preventData && numProduced % nproc == pid)
                {
                    var b = new byte[size];
                    random.NextBytes(b);
                    lastProduced = DateTime.Now;
                    return b;
                }
                else
                {
                    return Array.Empty<byte>();
                }
            }
            finally
            {
                lastProduced = DateTime.Now;
                numProduced++;
                currentBeginning += interval;
                currentEnd += interval;
            }
        }
    }
}
