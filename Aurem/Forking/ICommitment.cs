using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    /// <summary>
    /// Proves that a unit is below a unit that someone used in their forking proof.
    /// It might also contain parent hashes of the unit, to facilitate adding it to the DAG.
    /// </summary>
    public interface ICommitment
    {
        public IPreunit GetUnit();
        public Exception? CheckProof(ForkingProof fp);
        public ulong RmcID();
        public void SetParentHashes(byte[] ph);
        public Hash GetParentHash(ushort pid);
        public byte[] Marshal();
    }
}
