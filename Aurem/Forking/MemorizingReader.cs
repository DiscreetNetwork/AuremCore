using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    internal class MemorizingReader : Stream
    {
        private Stream? s;
        private MemoryStream _ms;
        private Conn? ms_Conn;

        private bool ms_isConn;

        public MemorizingReader(Stream s)
        {
            this.s = s;
            _ms = new MemoryStream();
            ms_Conn = null;
            ms_isConn = false;
        }

        public MemorizingReader(Conn conn)
        {
            s = null;
            _ms = new MemoryStream();
            ms_Conn = conn;
            ms_isConn = true;
        }

        public override bool CanRead => ms_isConn || s!.CanRead;

        public override bool CanSeek => !ms_isConn && s!.CanSeek;

        public override bool CanWrite => ms_isConn || s!.CanWrite;

        public override long Length => ms_isConn ? -1 : s!.Length;

        public override long Position
        {
            get
            {
                if (ms_isConn) return -1;
                return s!.Position;
            }
            set
            {
                if (ms_isConn) return;
                s!.Position = value;
            }
        }
        public override void Flush()
        {
            if (ms_isConn) ms_Conn!.Flush();
            else s!.Flush();
        }

        public override int Read(Span<byte> buffer)
        {
            if (!ms_isConn)
            {
                var i = s!.Read(buffer);
                _ms.Write(buffer);
                return i;
            }
            else
            {
                var arr = buffer.ToArray();
                var i = ms_Conn!.Read(arr).GetAwaiter().GetResult();
                arr.CopyTo(buffer);
                _ms.Write(buffer);
                return i;
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!ms_isConn)
            {
                var i = await s!.ReadAsync(buffer, offset, count, cancellationToken);
                _ms.Write(buffer, offset, count);
                return i;
            }
            else
            {
                var l = (count + offset > buffer.Length ? buffer.Length - offset : count);
                var i = await ms_Conn!.Read(buffer[offset..l]);
                _ms.Write(buffer, offset, count);
                return i;
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            if (!ms_isConn)
            {
                var i = await s!.ReadAsync(buffer, token);
                _ms.Write(buffer.Span);
                return i;
            }
            else
            {
                var arr = buffer.Span.ToArray();
                var i = await ms_Conn!.Read(arr);
                arr.CopyTo(buffer);
                _ms.Write(arr);
                return i;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!ms_isConn)
            {
                var i = s!.Read(buffer, offset, count);
                _ms.Write(buffer, offset, i);
                return i;
            }
            else
            {
                var l = (count + offset > buffer.Length ? buffer.Length - offset : count);
                var i = ms_Conn!.Read(buffer[offset..l]).GetAwaiter().GetResult();
                _ms.Write(buffer, offset, i);
                return i;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (ms_isConn) return -1;
            return s!.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (ms_isConn) return;
            s!.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("writes are not supported on MemorizingReader");
        }

        public byte[] GetMemory() => _ms.ToArray();
    }
}
