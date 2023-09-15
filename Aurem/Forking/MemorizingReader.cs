using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    internal class MemorizingReader : Stream
    {
        private Stream s;
        private MemoryStream _ms;

        public MemorizingReader(Stream s)
        {
            this.s = s;
            _ms = new MemoryStream();
        }

        public override bool CanRead => s.CanRead;

        public override bool CanSeek => s.CanSeek;

        public override bool CanWrite => s.CanWrite;

        public override long Length => s.Length;

        public override long Position { get => s.Position; set => s.Position = value; }

        public override void Flush()
        {
            s.Flush();
        }

        public override int Read(Span<byte> buffer)
        {
            var i = base.Read(buffer);
            _ms.Write(buffer);
            return i;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var i = s.Read(buffer, offset, count);
            _ms.Write(buffer, offset, i);
            return i;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return s.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            s.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
        }

        public byte[] GetMemory() => _ms.ToArray();
    }
}
