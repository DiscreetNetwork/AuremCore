using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    /// <summary>
    /// A commitment to a parent of a unit to which we have another commitment.
    /// </summary>
    public class InferredCommitment : ICommitment
    {
        private readonly object _lock = new object();

        public IPreunit Pu;
        public ICommitment ChildCommitment;
        public byte[] Encoded;
        public byte[] ParentHashes;

        public byte[] Marshal() => Encoded;

        public ulong RmcID() => ChildCommitment.RmcID();

        public IPreunit GetUnit() => Pu;

        public Exception? CheckProof(ForkingProof fp) => ChildCommitment.CheckProof(fp);

        public void SetParentHashes(byte[] ph)
        {
            lock (_lock)
            {
                ParentHashes = ph;
            }
        }

        public Hash GetParentHash(ushort pid)
        {
            lock (_lock)
            {
                var result = new Hash(new byte[32]);
                var i = pid * 32;
                if (i >= (ParentHashes?.Length ?? 0)) return Hash.Empty;

                Array.Copy(ParentHashes, i, result.Data, 0, 32);
                return result;
            }
        }
    }
}
