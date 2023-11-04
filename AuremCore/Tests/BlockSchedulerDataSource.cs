﻿using AuremCore.Core;
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

        public BlockSchedulerDataSource(int size, TimeSpan interval, int nproc, int pid)
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

            //if (pid == 0)
            //{
            //    Console.WriteLine($"PID {pid} blockscheduler start: {currentBeginning:HH:mm:ss.ffff}");
            //    Console.WriteLine($"PID {pid} blockscheduler end: {currentEnd:HH:mm:ss.ffff}");
            //}
        }

        public async Task<byte[]> Get()
        {
            // 0 goes first, then 1, then 2, etc...
            if (lastProduced == DateTime.MinValue)
            {
                // update the beginning
                while (currentEnd < DateTime.UtcNow)
                {
                    currentBeginning += interval;
                    currentEnd += interval;
                }
            }

            try
            {
                while (DateTime.UtcNow < currentBeginning) await Task.Delay(10);
                if (numProduced % nproc == pid)
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
