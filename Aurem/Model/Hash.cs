using BN256Core.Extensions;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Represents a hash of 256 bits in length.
    /// </summary>
    public struct Hash : IComparable<Hash>
    {
        /// <summary>
        /// The underlying bytes of the hash.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The length of the hash, in bytes. Must be 32 to be valid.
        /// </summary>
        public int Length => Data.Length;

        /// <summary>
        /// Creates a hash from the given byte array (note: this data must correspond to a hash). The hash data must be 32 bytes in length.
        /// </summary>
        /// <param name="data">The hash to wrap in this object.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public Hash(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            Data = data;
            if (Length != 32) throw new ArgumentException("data must be 32 bytes in length");
        }

        /// <summary>
        /// Returns a base64-encoded string of the first 8 bytes of the hash to be used as a human-readable identifier.
        /// </summary>
        /// <returns>a base64-encoded string of the first 8 bytes of the hash.</returns>
        public string Short() => Convert.ToBase64String(Data[0..8]);

        /// <summary>
        /// Returns whether or not the hash is lexographically less than the given hash.
        /// </summary>
        /// <param name="b">The hash to compare against.</param>
        /// <returns></returns>
        public bool LessThan(Hash b) => Data.Compare(b.Data) < 0;

        /// <summary>
        /// Compares the two hashes in lexographic order.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(Hash a, Hash b) => a.Data.Compare(b.Data);

        public int CompareTo(Hash other) => Compare(this, other);

        public static bool operator ==(Hash a, Hash b) => Compare(a, b) == 0;

        public static bool operator !=(Hash a, Hash b) => Compare(a, b) != 0;

        public static bool operator >(Hash a, Hash b) => Compare(a, b) > 0;

        public static bool operator <(Hash a, Hash b) => Compare(a, b) < 0;

        public static bool operator >=(Hash a, Hash b) => Compare(a, b) >= 0;

        public static bool operator <=(Hash a, Hash b) => Compare(a, b) <= 0;


        public static implicit operator Span<byte>(Hash a) => a.Data;


        public static implicit operator byte[](Hash a) => a.Data;

        public static implicit operator ReadOnlySpan<byte>(Hash a) => a.Data;


        /// <summary>
        /// Performs an exlusive or operation between the bits of the given hashes.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Hash XOR(Hash a, Hash b)
        {
            var res = new Hash(new byte[a.Data.Length]);
            
            for (int i = 0; i < a.Data.Length; i++)
            {
                if (i >= b.Data.Length) break;
                res.Data[i] = (byte)(a.Data[i] ^ b.Data[i]);
            }

            return res;
        }

        /// <summary>
        /// Performs an exlusive or operation with the bits of the given hash, and stores the result in the current hash.
        /// </summary>
        /// <param name="b"></param>
        public void XOREqual(Hash b)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                if (i >= b.Data.Length) break;
                Data[i] ^= b.Data[i];
            }
        }

        /// <summary>
        /// A hash object with all zero bits.
        /// </summary>
        public static Hash ZeroHash = new Hash(new byte[32]);

        /// <summary>
        /// Combines the hashes by writing them to a buffer in order of occurrence, then hashing the contents of the buffer.
        /// </summary>
        /// <param name="hashes">The hashes to combine.</param>
        /// <returns></returns>
        public static Hash CombineHashes(IEnumerable<Hash> hashes)
        {
            using var _ms = new MemoryStream();
            
            foreach (var h in hashes)
            {
                if (h.Data != null) _ms.Write(h.Data);
                else _ms.Write(ZeroHash.Data);
            }

            return new Hash(SHA256.HashData(_ms.ToArray()));
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null && Data == null) return true;

            if (obj is Hash hash)
            {
                return Data.Compare(hash.Data) == 0;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(Data, 0);
        }

        public override string ToString()
        {
            return Convert.ToBase64String(Data);
        }

        /// <summary>
        /// Empty represents a null hash; i.e. a null value for the hash.
        /// </summary>
        public static Hash Empty = new Hash { Data = null };

        public class HashComparer: IComparer<Hash>
        {
            public int Compare(Hash x, Hash y) => Hash.Compare(x, y);
        }

        public class HashEqualityComparer: IEqualityComparer<Hash>
        {
            public int GetHashCode(Hash obj) => obj.GetHashCode();
            public bool Equals(Hash x, Hash y) => x.Equals(y);
        }
    }
}
