using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public interface IBlockSource
    {
        public Task<Block> Get();
    }
}
