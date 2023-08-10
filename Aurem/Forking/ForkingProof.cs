using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    /// <summary>
    /// Consists of two units, pu and pv, created by the same process at the same height, and a third unit, pcommit, representing the
    /// unit the creator of the proof commits to be the last unit created by the forker the creator will build on directly.
    /// The last unit may be null.
    /// </summary>
    public class ForkingProof
    {
        public IPreunit Pu;
        public IPreunit Pv;
        public IPreunit PCommit;

        public byte[] Encoded;

        public ForkingProof(IPreunit u, IPreunit v, IPreunit max)
        {
            if (u.Hash().Equals(v.Hash()))
            {
                Pu = null;
                Pv = null;
                PCommit = null;
                Encoded = null;
                return;
            }

            using var ms = new MemoryStream();
            EncodeUtil.WritePreunit(u, ms);
            EncodeUtil.WritePreunit(v, ms);
            EncodeUtil.WritePreunit(max, ms);
            ms.Position = 0;
            Encoded = ms.ToArray();
            Pu = EncodeUtil.ReadPreunit(ms);
            Pv = EncodeUtil.ReadPreunit(ms);
            PCommit = EncodeUtil.ReadPreunit(ms);
        }

        public byte[] Marshal() => Encoded;

        public ForkingProof Unmarshal(byte[] data)
        {
            using var ms = new MemoryStream(data);
            Encoded = new byte[data.Length];
            Array.Copy(data, Encoded, data.Length);

            Pu = EncodeUtil.ReadPreunit(ms);
            Pv = EncodeUtil.ReadPreunit(ms);
            PCommit = EncodeUtil.ReadPreunit(ms);

            return this;
        }

        public ushort ForkerID() => Pu.Creator();

        /// <summary>
        /// Returns the encoded proof in two parts: first the proof itself, then the commitment.
        /// </summary>
        /// <returns></returns>
        public (byte[], byte[]) SplitEncoding()
        {
            using var ms = new MemoryStream(Encoded);
            EncodeUtil.ReadPreunit(ms);
            EncodeUtil.ReadPreunit(ms);
            var proofOnly = Encoded[0..(int)ms.Position];
            var commitOnly = Encoded[(int)ms.Position..];
            return (proofOnly, commitOnly);
        }

        /// <summary>
        /// Replaces the commit in this proof. Used to create our own alert when we don't know the units in the original alert.
        /// </summary>
        /// <param name="commit"></param>
        public void ReplaceCommit(IUnit commit)
        {
            (var proofOnly, _) = SplitEncoding();
            var comme = EncodeUtil.EncodeUnit(commit);
            Encoded = proofOnly.Concat(comme).ToArray();
            PCommit = EncodeUtil.DecodeUnit(comme);
        }

        public Exception? CheckCorrectness(ushort expectedPID, IPublicKey key)
        {
            if (Pu == null || Pv == null) return new Exception("null units do not prove forking");
            if (Pu.Creator() != expectedPID || Pv.Creator() != expectedPID || (PCommit != null && PCommit.Creator() != expectedPID)) return new Exception("creator differs from expected");
            if (!key.Verify(Pu) || !key.Verify(Pv) || (PCommit != null && !key.Verify(PCommit))) return new Exception("improper signature");
            if (Pu.Height() != Pv.Height()) return new Exception("two units on different heights do not prove a fork");
            if (Pu.Hash().Equals(Pv.Hash())) return new Exception("two copies of a unit are not a fork");

            return null;
        }

        public ICommitment ExtractCommitment(ulong rmcID)
        {
            (_, var commitOnly) = SplitEncoding();
            return new BaseCommitment(PCommit, commitOnly, rmcID);
        }
    }
}
