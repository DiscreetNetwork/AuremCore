using AuremCore.Crypto.BN256;
using AuremCore.Crypto.BN256.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Multi
{
    public class Keychain
    {
        private VerificationKey[] pubs;
        private SecretKey secret;
        private ushort pid;

        public Keychain(VerificationKey[] pubs, SecretKey secret)
        {
            var ours = secret.VerificationKey().Marshal();
            
            this.pid = (ushort)pubs.Zip(Enumerable.Range(0, pubs.Length)).Select(x => (x.First.Marshal(), x.Second)).Where(x => x.Item1.BEquals(ours)).First().Second;
            this.secret = secret;
            this.pubs = pubs;
        }

        public Keychain(IEnumerable<VerificationKey> pubs, SecretKey secret) : this(pubs.ToArray(), secret) { }

        public bool Verify(ushort pid, byte[] data)
        {
            if (data.Length < Constants.SignatureLength)
            {
                return false;
            }

            var dataEnd = data.Length - Constants.SignatureLength;
            var sigdat = data.AsSpan(dataEnd, Constants.SignatureLength);
            
            Signature sig = new Signature().Unmarshal(sigdat);
            return pubs[pid].Verify(sig, data.AsSpan(0, dataEnd)); 
        }

        public byte[] Sign(byte[] data)
        {
            return secret.Sign(data).Marshal();
        }

        public bool MultiVerify(MultiSignature s)
        {
            if (!s.Complete()) return false;

            VerificationKey multiKey = new VerificationKey();
            foreach (var c in s.collected.Keys)
            {
                multiKey = VerificationKey.Add(multiKey, pubs[c]);
            }

            return multiKey.Verify(s.sgn, s.data);
        }

        public ushort Pid() => this.pid;

        public int Length => pubs.Length;
    }
}
