using Aurem.Model;
using AuremCore.Crypto.Threshold;
using BN256Core;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Creating
{
    public static class ProofUtil
    {
        /// <summary>
        /// A proof is a message required to verify if an epoch has finished.
        /// It consists of the ID and hash of the last timing unit in the epoch.
        /// This message is signed with a threshold signature.
        /// </summary>
        public static readonly int ProofLength = Hash.ZeroHash.Length + 8;

        /// <summary>
        /// Checks if the given preunit is a proof that a new epoch started.
        /// </summary>
        public static bool EpochProof(IPreunit pu, WeakThresholdKey wtk)
        {
            if (pu.Dealing() || wtk == null) return false;
            if (pu.EpochID() == 0) return true;
            try
            {
                (var sig, var msg) = DecodeSignature(pu.Data());
                (_, _, var epoch, _) = DecodeProof(msg);
                if (epoch+1 != pu.EpochID()) return false;

                return wtk.VerifySignature(sig, msg);
            }
            catch { return false; }
        }

        /// <summary>
        /// Produces an encoded form of the EpochProof (which proves the epoch ended).
        /// The proof consists of a little-endian encoded ID followed by the hash bytes of the last timing unit of the epoch.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static byte[] EncodeProof(IUnit u)
        {
            var msg = new byte[ProofLength];
            BinaryPrimitives.WriteUInt64LittleEndian(msg, u.UnitID());
            Array.Copy(u.Hash().Data, 0, msg, 8, Hash.ZeroHash.Length);
            return msg;
        }

        /// <summary>
        /// Takes an encoded EpochProof and decodes it into a tuple (Height, Creator, Epoch, Hash) of the last timing unit of the epoch.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static (int Height, ushort Creator, uint Epoch, Hash Hash) DecodeProof(ReadOnlySpan<byte> data)
        {
            if (data.Length == ProofLength)
            {
                var id = BinaryPrimitives.ReadUInt64LittleEndian(data);
                Hash hash = new Hash(data.Slice(0, Hash.ZeroHash.Length).ToArray());
                (var h, var c, var e) = IPreunit.DecodeID(id);
                return (h, c, e, hash);
            }

            return (-1, 0, 0, Hash.Empty);
        }

        /// <summary>
        /// Converts signature share and the signed message into a byte array that can be put into a unit.
        /// </summary>
        /// <param name="share"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] EncodeShare(Share share, byte[] msg)
        {
            var sh = share.Marshal();
            var result = new byte[sh.Length + msg.Length];
            Array.Copy(msg, result, msg.Length);
            Array.Copy(sh, 0, result, msg.Length, sh.Length);

            return result;
        }

        /// <summary>
        /// Reads signature share and the signed message from the data contained in some unit.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static (Share, byte[]) DecodeShare(ReadOnlySpan<byte> data)
        {
            var share = new Share();
            share.Unmarshal(data[ProofLength..]);

            return (share, data[0..ProofLength].ToArray());
        }

        /// <summary>
        /// Converts signature and the signed message into a byte array that can be put into a unit.
        /// </summary>
        /// <param name="sig"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] EncodeSignature(Signature sig, byte[] msg)
        {
            var sg = sig.Marshal();
            var result = new byte[msg.Length + sg.Length];
            Array.Copy(msg, result, msg.Length);
            Array.Copy(sg, 0, result, msg.Length, sg.Length);

            return result;
        }

        /// <summary>
        /// Reads signature and the signed message from the data contained in some unit.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static (Signature Sig, byte[] Msg) DecodeSignature(ReadOnlySpan<byte> data)
        {
            var sig = new Signature();
            sig.Unmarshal(data[ProofLength..]);

            return (sig, data[0..ProofLength].ToArray());
        }
    }
}
