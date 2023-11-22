using Aurem.Crypto.Signing;
using Aurem.Model;
using AuremCore.Crypto.P2P;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Config
{
    /// <summary>
    /// Represents the public data about a committee known before the algorithm starts.
    /// </summary>
    public class Committee
    {
        /// <summary>
        /// The public keys of all committee members, ordered according to process IDs.
        /// </summary>
        public List<IPublicKey> PublicKeys { get; set; }

        /// <summary>
        /// Public keys of all committee members used for generating keys for p2p communication. Ordered according to process IDs.
        /// </summary>
        public List<P2PPublicKey> P2PPublicKeys { get; set; }

        /// <summary>
        /// Verification keys of all committee members used for RMC, ordered by process IDs.
        /// </summary>
        public List<VerificationKey> RMCVerificationKeys { get; set; }

        /// <summary>
        /// SetupAddresses of all committee members.
        /// </summary>
        public Dictionary<string, List<string>> SetupAddresses { get; set; }

        /// <summary>
        /// Addresses of all committee members.
        /// </summary>
        public Dictionary<string, List<string>> Addresses { get; set; }

        private static readonly string MalformedData = "malformed committee data";

        public static Dictionary<string, string> ParseAddresses(string addrList)
        {
            var addrs = new Dictionary<string, string>();
            foreach (var addr in addrList.Split(' '))
            {
                if (string.IsNullOrEmpty(addr)) continue;
                switch (addr[0])
                {
                    case 'r':
                        addrs["rmc"] = addr.Substring(1);
                        break;
                    case 'f':
                        addrs["fetch"] = addr.Substring(1);
                        break;
                    case 'g':
                        addrs["gossip"] = addr.Substring(1);
                        break;
                    case 'm':
                        addrs["mcast"] = addr.Substring(1);
                        break;
                }
            }

            return addrs;
        }

        public static (string Pubkey, string P2PPubkey, string Verkey, Dictionary<string, string> SetupAddrs, Dictionary<string, string> Addrs) ParseCommitteeLine(string line)
        {
            var s = line.Split('|');
            if (s == null || s.Length < 5)
            {
                throw new FormatException("commitee line should be of the form: \"publicKey|verifiactionKey|p2pPublicKey|setupAddresses|addresses\"");
            }
            var pk = s[0];
            var p2pPk = s[1];
            var vk = s[2];
            var setupAddrs = s[3];
            var addrs = s[4];
            var errStrings = new List<string>();
            if (string.IsNullOrEmpty(pk))
            {
                errStrings.Add(MalformedData);
                errStrings.Add("public key should be non-empty");
            }

            if (string.IsNullOrEmpty(p2pPk)) { errStrings.Add("P2P public key should be non-empty"); }
            if (string.IsNullOrEmpty(vk)) { errStrings.Add("verification key should be non-empty"); }

            if (errStrings.Count > 0)
            {
                throw new FormatException(errStrings.Aggregate("", (x, y) => x + "\n" + y).Trim());
            }

            return (pk, p2pPk, vk, ParseAddresses(setupAddrs), ParseAddresses(addrs));
        }

        public static void AddAddress(Dictionary<string, List<string>> CommitteeAddressMap, Dictionary<string, string> AddressMap)
        {
            foreach (var kvpair in AddressMap)
            {
                bool success = CommitteeAddressMap.TryGetValue(kvpair.Key, out var addressList);
                if (success)
                {
                    addressList.Add(kvpair.Value);
                }
                else
                {
                    CommitteeAddressMap[kvpair.Key] = new List<string> { kvpair.Value };
                }
            }
        }

        public static Committee LoadCommittee(Stream s)
        {
            var r = new StreamReader(s);
            var c = new Committee 
            {
                PublicKeys = new List<IPublicKey>(),
                P2PPublicKeys = new List<P2PPublicKey>(),
                RMCVerificationKeys = new List<VerificationKey>(),
                SetupAddresses = new Dictionary<string, List<string>>(),
                Addresses = new Dictionary<string, List<string>>()
            };

            while (!r.EndOfStream)
            {
                (var pk, var p2pPk, var vk, var setupAddrs, var addrs) = ParseCommitteeLine(r.ReadLine() ?? "");

                var pubkey = SPublicKey.DecodePublicKey(pk);
                var p2pPubkey = P2PPublicKey.Decode(p2pPk);
                var verificationKey = VerificationKey.DecodeVerificationKey(vk);

                c.PublicKeys.Add(pubkey);
                c.P2PPublicKeys.Add(p2pPubkey);
                c.RMCVerificationKeys.Add(verificationKey);
                AddAddress(c.SetupAddresses, setupAddrs);
                AddAddress(c.Addresses, addrs);
            }

            if (c.PublicKeys.Count < 4)
            {
                throw new Exception(MalformedData);
            }

            return c;
        }

        public static void StoreAddresses(StreamWriter s, int pid, Dictionary<string, List<string>> addresses, IList<string> types)
        {
            var w = s;

            for (int j = 0; j < types.Count; j++)
            {
                var syncType = types[j];

                if (!addresses.ContainsKey(syncType)) continue;

                if (j != 0) w.Write(" ");
                w.Write(syncType[0]);
                w.Write(addresses[syncType][pid]);
            }
        }

        public void StoreCommittee(Stream s)
        {
            var w = new StreamWriter(s);

            for (int i = 0; i < PublicKeys.Count; i++)
            {
                w.Write(PublicKeys[i].Encode() + "|");
                w.Write(P2PPublicKeys[i].Encode() + "|");
                w.Write(RMCVerificationKeys[i].Encode() + "|");
                StoreAddresses(w, i, SetupAddresses, new string[] { "rmc", "fetch", "gossip" });
                w.Write("|");
                StoreAddresses(w, i, Addresses, new string[] { "rmc", "mcast", "fetch", "gossip" });
                w.Write("\n");
            }

            w.Flush();
        }
    }
}
