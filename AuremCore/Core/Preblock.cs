using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public class Preblock
    {
        public List<byte[]> Data;
        public byte[] RandomBytes;

        public Preblock(List<byte[]> data, byte[] randomBytes)
        {
            Data = data;
            RandomBytes = randomBytes;
        }
    }
}
