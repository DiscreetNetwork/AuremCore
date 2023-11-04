using Aurem.Common;
using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    /// <summary>
    /// BaseCommitment is a commitment directly to the unit included in a forking proof.
    /// </summary>
    public class BaseCommitment : ICommitment
    {
        private readonly object _lock = new object();

        public IPreunit Pu;
        public ulong Id;
        public byte[] Encoded;
        public byte[] ParentHashes;

        public BaseCommitment(IPreunit pu, byte[] puEncoded, ulong rmcID) 
        {
            Id = rmcID;
            
            Encoded = new byte[puEncoded.Length + 8];
            BinaryPrimitives.WriteUInt64LittleEndian(Encoded, rmcID);
            Array.Copy(puEncoded, 0, Encoded, 8, puEncoded.Length);

            Pu = pu;
        }

        public byte[] Marshal() => Encoded;

        public ulong RmcID() => Id;

        public IPreunit GetUnit() => Pu;

        public Exception? CheckProof(ForkingProof fp)
        {
            var cu = GetUnit();
            if (cu != null)
            {
                if (!cu.Hash().Equals(fp.PCommit.Hash())) return new Exception("wrong proof for commit");
                return null;
            }

            return new Exception("unitless commitment");
        }

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
                var result = new byte[32];
                int i = pid * 32;

                if (i >= (ParentHashes?.Length ?? 0))
                {
                    return Hash.Empty;
                }

                Array.Copy(ParentHashes, i, result, 0, 32);
                return new Hash(result);
            }
        }

        public static (ICommitment?, Exception?) CommitmentForPreparent(ICommitment comm, IPreunit pu, IList<Hash> hashes, byte[] encoded)
        {
            var cu = comm.GetUnit();
            if (cu == null) return (null, new Exception("empty commitment cannot justify parents"));
            
            var pid = cu.Creator();
            if (pid != pu.Creator()) return (null, new Exception("cannot justify unit created by a different process"));

            if (hashes == null || hashes.Count <= pid || !hashes[pid].Equals(pu.Hash())) return (null, new Exception("cannot justify unit with a mismatched hash"));

            if (!cu.View().ControlHash.Equals(Hash.CombineHashes(hashes))) return (null, new Exception("control hash does not match hashes of parents"));

            var parEncoded = new byte[32*hashes.Count];
            for (int i = 0; i < parEncoded.Length; i += 32)
            {
                if (hashes[(i >> 5)].Equals(Hash.Empty)) Array.Copy(Hash.ZeroHash.Data, 0, parEncoded, i, 32);
                else Array.Copy(hashes[(i >> 5)].Data, 0, parEncoded, i, 32);
            }

            comm.SetParentHashes(parEncoded);
            return (new InferredCommitment { Pu = pu, ChildCommitment = comm, Encoded = encoded }, null);
        }

        public static (ICommitment?, Exception?) CommitmentForParent(ICommitment comm, IUnit u)
        {
            var cu = comm.GetUnit();
            if (cu == null) return (null, new Exception("empty commitment cannot justify parents"));

            if (u == null || !cu.Hash().Equals(u.Hash())) return (null, new Exception("incorrect commitment unit supplied"));

            var pred = u.Predecessor();
            var enc = comm.Marshal();
            var predEncoded = EncodeUtil.EncodeUnit(pred);
            var encoded = enc.Concat(predEncoded).ToArray();
            
            var parEncoded = new byte[32*u.Parents().Count];
            foreach ((var i, var p) in u.Parents().Index())
            {
                if (p == null) Array.Copy(Hash.ZeroHash.Data, 0, parEncoded, (i << 5), 32);
                else Array.Copy(p.Hash().Data, 0, parEncoded, (i << 5), 32);
            }

            encoded = encoded.Concat(parEncoded).ToArray();
            var pu = EncodeUtil.DecodeUnit(predEncoded);

            comm.SetParentHashes(parEncoded);
            return (new InferredCommitment { Pu = pu, ChildCommitment = comm, Encoded = encoded }, null);
        }
    }
}
