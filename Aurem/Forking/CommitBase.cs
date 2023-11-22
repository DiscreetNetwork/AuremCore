using Aurem.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    public class CommitBase
    {
        private readonly object _lock = new object();

        public Dictionary<Hash, ICommitment> ToUnit;
        public Dictionary<ushort, Dictionary<ushort, ICommitment>> ByMember;
        
        public CommitBase()
        {
            ToUnit = new Dictionary<Hash, ICommitment>();
            ByMember = new Dictionary<ushort, Dictionary<ushort, ICommitment>>();
        }

        public void Add(ICommitment c, ushort commiter, ushort forker)
        {
            var cu = c.GetUnit();

            lock (_lock)
            {
                if (cu != null)
                {
                    var h = cu.Hash();
                    if (!ToUnit.ContainsKey(h) || ToUnit[h] == null || ToUnit[h].GetParentHash(0).Equals(Hash.Empty))
                    {
                        ToUnit[h] = c;
                    }
                }

                if (!ByMember.ContainsKey(forker) || ByMember[forker] == null)
                {
                    ByMember[forker] = new();
                }

                if (!ByMember[forker].ContainsKey(commiter) || ByMember[forker][commiter] == null)
                {
                    ByMember[forker][commiter] = c;
                }
            }
        }

        public ICommitment GetByHash(Hash h)
        {
            lock (_lock)
            {
                return ToUnit.ContainsKey(h) ? ToUnit[h] : null!;
            }
        }

        public ICommitment GetByParties(ushort commiter, ushort forker)
        {
            lock (_lock)
            {
                if (!ByMember.ContainsKey(forker) || ByMember[forker] == null || !ByMember[forker].ContainsKey(commiter)) return null;
                return ByMember[forker][commiter];
            }
        }

        public bool IsForker(ushort forker)
        {
            lock (_lock)
            {
                return ByMember.ContainsKey(forker) && ByMember[forker] != null;
            }
        }

        public Exception? AddBatch(IList<ICommitment> comms, ForkingProof proof, ushort commiter)
        {
            if (comms == null || comms.Count == 0) return new Exception("given commitment batch was empty");

            var err = comms[0].CheckProof(proof);
            if (err != null) return err;

            var forker = proof.PCommit.Creator();
            foreach (var c in comms)
            {
                Add(c, commiter, forker);
            }

            return null;
        }
    }
}
