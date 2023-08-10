using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    public static class ForkingUtil
    {
        public static (List<ICommitment>?, Exception?) AcquireCommitments(Stream s)
        {
            try
            {
                var mr = new MemorizingReader(s);

                var buf = new byte[8];
                mr.Read(buf, 0, 8);
                var rmcID = BinaryPrimitives.ReadUInt64LittleEndian(buf);
                var pu = EncodeUtil.ReadPreunit(mr);

                var comm = (ICommitment)new BaseCommitment(pu, mr.GetMemory(), rmcID);

                var result = new List<ICommitment> { comm };
                pu = EncodeUtil.ReadPreunit(mr);

                while (pu != null)
                {
                    var hashes = new List<Hash>();

                    foreach (var _ in pu.View().Heights)
                    {
                        var h = new Hash(new byte[32]);
                        mr.Read(h.Data);
                        hashes.Add(h);
                    }

                    (comm, var err) = BaseCommitment.CommitmentForPreparent(comm, pu, hashes, mr.GetMemory());
                    if (err != null) return (null, err);

                    result.Add(comm);
                    pu = EncodeUtil.ReadPreunit(mr);
                }

                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }
    }
}
