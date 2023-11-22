using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    // interpreter represents a process for interpreting preblocks into blocks
    public interface IInterpreter : IService
    {
        public void Set(IPreblockSource source);
    }

    public delegate (IInterpreter, IBlockSource) BuildInterpreter(Stream config, ILogger logger);
}
