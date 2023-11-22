using AuremCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public class RandomDataSource : IDataSource
    {
        private int size;
        private Random random;

        public RandomDataSource(int size)
        {
            this.size = size;
            random = new Random();
        }

        public Task<byte[]> Get()
        {
            var b = new byte[size];
            random.NextBytes(b);
            return Task.FromResult(b);
        }
    }
}
