using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    // validator represents a process that accepts data and pushes it to the orderer, while waiting for blocks from the interpeter
    public interface IValidator : IService
    {
        public void Set(IBlockSource source);
    }

    public delegate (IValidator, IDataSource) BuildValidator(Stream config, ILogger logger);
}
