using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    /// <summary>
    /// Implements "virtual connections", many of which utilize the same underlying TCP link.
    /// Each virtual connection has a unique ID and every piece of data sent through the common TCP link is prefixed with a 12-byte header consisting of the ID and length of data.
    /// ID is 8 bytes, and the length is 4 bytes.
    /// All writes are buffered and the actual network traffic happens only on Flush() (explicit) or when the buffer is full.
    /// Reads are also buffered and they read bytes from the stream populated by the link supervising the connection.
    /// Close() sends a header with data length zero. After closing the connection, calling Write() or Flush() throws an error, but reading is still possible until the underlying stream is depleted.
    /// 
    /// Note: Write() and Flush() might not be thread safe.
    /// </summary>
    public class NetReader
    {
    }
}
