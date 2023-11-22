using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremTests.Packets
{
    internal class ErrorsPacket
    {
        public List<string> Errors { get; private set; }
        public int Count => Errors.Count;

        public ErrorsPacket(List<string> errors)
        {
            Errors = errors;
        }

        public ErrorsPacket(byte[] b)
        {
            Deserialize(b);
        }

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(Errors.Count);
            for (int i = 0; i < Errors.Count; i++)
            {
                bw.Write(Errors[i]);
            }

            bw.Flush();
            return ms.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);

            Errors = new();

            var count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Errors.Add(br.ReadString());
            }
        }
    }
}
