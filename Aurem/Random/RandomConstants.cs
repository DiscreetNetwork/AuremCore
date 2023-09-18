using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Random
{
    public static class RandomConstants
    {
        public static readonly int DealingLevel = 0;
        public static readonly int VotingLevel = 3;
        public static readonly int MultikeyLevel = 6;
        public static readonly int SharesLevel = 8;

        public static byte[] Nonce(int level)
        {
            var data = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(data, (ushort)level);
            return data;
        }
    }
}
