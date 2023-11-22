using DiscreetCoreLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Core
{
    public class AuthorityClient
    {
        public List<Key> Keystore;
        private Key authorityKey;
        private int authorityId;

        public static ulong TimestampToSlot(DateTime time, DateTime genesis, TimeSpan duration)
        {
            var t = time.Subtract(genesis).Milliseconds / duration.Milliseconds;
            return (ulong)t;
        }

        public static Key SlotAuthor(ulong slot, IList<Key> authorities)
        {
            var idx = slot % (ulong)authorities.Count;
            var currentAuthor = authorities[(int)idx];

            return currentAuthor;
        }

        //public static (Key, Key) ClaimSlot(ulong slot, IList<Key> authorities, )
    }
}
