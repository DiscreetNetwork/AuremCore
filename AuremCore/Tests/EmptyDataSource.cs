using AuremCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public class EmptyDataSource : IDataSource
    {
        private static readonly Task<byte[]> _res = Task.FromResult(Array.Empty<byte>());

        public EmptyDataSource() { }

        public Task<byte[]> Get()
        {
            return _res;
        }
    }
}
