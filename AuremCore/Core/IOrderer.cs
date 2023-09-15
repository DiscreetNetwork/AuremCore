using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    // Orderer represents a process used for ordering data into preblocks
    public interface IDataOrderer : IService
    {
        public void Set(IDataSource source);
    }

    public delegate (IDataOrderer, IPreblockSource) BuildOrderer(Stream configStream, ILogger logger);
}
