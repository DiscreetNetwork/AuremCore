using AuremCore.Crypto.BN256;
using AuremCore.Crypto.BN256.Extensions;
using AuremCore.Crypto.Encrypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Threshold
{
    public class ThresholdKey
    {
        internal ushort owner;
        internal ushort dealer;
        internal ushort threshold;
        internal VerificationKey globalVK;
        internal VerificationKey[] vks;
        internal byte[][] encSKs;
        internal SecretKey sk;

        public ThresholdKey() { }

        public ushort Threshold => threshold;

        public virtual Share CreateShare(byte[] msg)
        {
            return new Share { owner = this.owner, sig = sk.Sign(msg) };
        }

        public (Signature?, bool) CombineShares(Share[] shares)
        {
            if (shares.Length > threshold)
            {
                shares = shares.AsSpan(0, threshold).ToArray();
            }

            if (shares.Length != threshold)
            {
                return (null, false);
            }

            long[] points = shares.Select(x => (long)x.owner).ToArray();
            Signature sum = shares.AsParallel().Select(sh =>
            {
                return Signature.Mul(sh.sig, new SecretKey(Util.Lagrange(points, sh.owner)));
            }).Aggregate(new Signature(), (x, y) => Signature.Add(x, y));

            return (sum, true);
        }

        public bool VerifyShare(Share share, byte[] msg)
        {
            return vks[share.owner].Verify(share.sig, msg);
        }

        public bool VerifySignature(Signature s, byte[] msg)
        {
            return globalVK.Verify(s, msg);
        }

        public bool PolyVerify(PolyVerifier pv)
        {
            return pv.Verify(vks);
        }

        public SecretKey? VerifySecretKey()
        {
            var vk = sk.VerificationKey();
            if (vk.Marshal().BEquals(vks[owner].Marshal()))
            {
                return sk;
            }

            return null;
        }

        /*
         * threshold (2) + 
         * 
         */
        public byte[] Encode()
        {
            MemoryStream ms = new();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(threshold);

            byte[] vkm = globalVK.Marshal();
            bw.Write(vkm.Length);
            bw.Write(vkm);

            bw.Write(vks.Length);
            for (int i = 0; i < vks.Length; i++)
            {
                byte[] vksm = vks[i].Marshal();
                bw.Write(vksm.Length);
                bw.Write(vksm);
            }

            for (int i = 0; i < (encSKs?.Length ?? 0); i++)
            {
                bw.Write(encSKs[i].Length);
                bw.Write(encSKs[i]);
            }

            return ((MemoryStream)bw.BaseStream).ToArray();
        }

        public static (ThresholdKey, bool) Decode(byte[] data, ushort dealer, ushort owner, SymmetricKey decryptionKey)
        {
            int idx = 6;
            if (data == null || data.Length < idx) throw new Exception("data too short");

            MemoryStream ms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(ms);

            var threshold = br.ReadUInt16();
            var gvklen = br.ReadInt32();

            idx += gvklen;
            if (data.Length < idx) throw new Exception("data too short");
            var globalVK = new VerificationKey().Unmarshal(br.ReadBytes(gvklen));

            idx += 4;
            if (data.Length < idx) throw new Exception("data too short");
            var nproc = (ushort)br.ReadInt32();
            var vks = new VerificationKey[nproc];

            for (int i = 0; i < nproc; i++)
            {
                idx += 4;
                if (data.Length < idx) throw new Exception("data too short");
                var vklen = br.ReadInt32();

                idx += vklen;
                if (data.Length < idx) throw new Exception("data too short");
                vks[i] = new VerificationKey().Unmarshal(br.ReadBytes(vklen));
            }

            var encSKs = new byte[nproc][];
            for (int i = 0; i < nproc; i++)
            {
                idx += 4;
                if (data.Length < idx) throw new Exception("data too short");
                var encsklen = br.ReadInt32();

                idx += encsklen;
                if (data.Length < idx) throw new Exception("data too short");
                encSKs[i] = br.ReadBytes(encsklen);
            }

            var sk = DecryptSecretKey(encSKs[owner], vks[owner], decryptionKey);

            return (new ThresholdKey
            {
                dealer = dealer,
                owner = owner,
                threshold = threshold,
                globalVK = globalVK,
                vks = vks,
                encSKs = encSKs,
                sk = sk
            }, true);
        }

        private static SecretKey DecryptSecretKey(byte[] data, VerificationKey vk, SymmetricKey decryptionKey)
        {
            var dec = decryptionKey.Decrypt(data);
            var sk = new SecretKey().Unmarshal(dec);

            if (!VerificationKey.VerifyKeys(vk, sk))
            {
                throw new Exception("secret key doesn't match with the verification key");
            }

            return sk;
        }
    }
}
