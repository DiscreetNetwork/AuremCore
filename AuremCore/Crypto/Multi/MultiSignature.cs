using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Multi
{
    public class MultiSignature
    {
        private SemaphoreSlim mutex = new(1, 1);
        internal ushort threshold;
        internal byte[] data;
        internal Signature sgn;
        internal Dictionary<ushort, bool> collected;

        public MultiSignature(ushort threshold, byte[] data)
        {
            this.threshold = threshold;
            this.data = data;
            this.collected = new();
        }

        public MultiSignature()
        {
        }

        public (bool, Exception?) Aggregate(ushort pid, byte[] sgnBytes)
        {
            Signature sig = new Signature().Unmarshal(sgnBytes);

            mutex.Wait();

            if (collected[pid])
            {
                return (Complete(), new Exception("second copy of signature"));
            }

            sgn = Signature.Add(sig, sgn);
            collected[pid] = true;

            mutex.Release();
            return (Complete(), null);
        }

        public byte[] Marshal()
        {
            mutex.Wait();
            MemoryStream ms = new MemoryStream();
            foreach (var c in collected.Keys)
            {
                ms.WriteByte((byte)(c & 0xff));
                ms.WriteByte((byte)(c >> 8));
            }

            ms.Write(sgn.Marshal());

            mutex.Release();

            return ms.ToArray();
        }

        public MultiSignature Unmarshal(byte[] data)
        {
            mutex.Wait();

            collected = new Dictionary<ushort, bool>();
            for (int i = 0; i < threshold; i++)
            {
                ushort c = (ushort)((data[2* i]) | (data[2* i+1] << 8));
                collected[c] = true;
            }

            sgn = new Signature().Unmarshal(data.AsSpan(2*threshold, Constants.SignatureLength));

            mutex.Release();
            return this;
        }

        public int Length { get => 2*threshold + Constants.SignatureLength; }

        public bool Complete()
        {
            return collected.Count >= (int)threshold;
        }
    }
}
