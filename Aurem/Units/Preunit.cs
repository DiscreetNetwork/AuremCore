using Aurem.Model;
using DiscreetCoreLib.Mnemonics.Wordlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Units
{
    /// <summary>
    /// Implements <see cref="IPreunit"/>.
    /// </summary>
    public class Preunit : IPreunit
    {
        private ushort _creator;
        private uint _epochId;
        private int _height;
        private byte[] _signature;
        private Hash _hash;
        private Crown _crown;
        private byte[] _data;
        private byte[] _rsData;

        public Preunit() { }

        public Preunit(ulong id, Crown crown, byte[] data, byte[] rsData, byte[] signature)
        {
            (var h, var creator, var epoch) = IPreunit.DecodeID(id);
            if (h != crown.Heights[creator]+1)
            {
                throw new ArgumentException("Inconsistent height information in preunit ID and crown", nameof(id));
            }

            _creator = creator;
            _epochId = epoch;
            _height = h;
            _signature = signature;
            _crown = crown;
            _data = data;
            _rsData = rsData;
            _hash = ComputeHash(id, crown, data, rsData);
        }

        public uint EpochID() => _epochId;

        public byte[] RandomSourceData() => _rsData;

        public byte[] Data() => _data;

        public ushort Creator() => _creator;

        public int Height() => _height;

        public byte[] Signature() => _signature;

        public Hash Hash() => _hash;

        public Crown View() => _crown;

        /// <summary>
        /// Calculates the value of the unit's hash based on the provided data.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="crown"></param>
        /// <param name="data"></param>
        /// <param name="rsData"></param>
        /// <returns></returns>
        public static Hash ComputeHash(ulong id, Crown crown, byte[] data, byte[] rsData)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(id);
            bw.Write(data);
            bw.Write(rsData);
            foreach (var h in crown.Heights)
            {
                if (h == -1) bw.Write(uint.MaxValue);
                else bw.Write((uint)h);
            }
            bw.Write(crown.ControlHash.Data);

            return new Hash(SHA256.HashData(ms.ToArray()));
        }
    }
}
