using Aurem.Config;
using Aurem.Crypto.Signing;
using Aurem.Model;
using AuremCore.Crypto.P2P;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremTests.Cmd
{
    public static class KeyGen
    {
        private class MemberKeys
        {
            public IPublicKey PublicKey { get; set; }
            
            public IPrivateKey PrivateKey { get; set; }

            public SecretKey SecKey { get; set; }

            public VerificationKey VerKey { get; set; }

            public P2PPublicKey P2PPubKey { get; set; }

            public P2PSecretKey P2PSecKey { get; set; }

            public Dictionary<string, List<string>> Addresses { get; set; }

            public MemberKeys(Dictionary<string, List<string>> addresses)
            {
                this.Addresses = addresses;
                (PublicKey, PrivateKey) = Keys.GenerateKeys();
                (VerKey, SecKey) = VerificationKey.GenerateKeys();
                (P2PPubKey, P2PSecKey) = P2P.GenerateKeys();
            }
        }

        public static void MakeAddressMap(string addrs, Dictionary<string, List<string>> addresses)
        {
            foreach (var addr in addrs.Split(" "))
            {
                if (string.IsNullOrEmpty(addr)) continue;

                switch (addr[0])
                {
                    case 'r':
                        addresses["rmc"]?.Add(addr[1..]); break;
                    case 'm':
                        addresses["mcast"]?.Add(addr[1..]); break;
                    case 'f':
                        addresses["fetch"]?.Add(addr[1..]); break;
                    case 'g':
                        addresses["gossip"]?.Add(addr[1..]); break;
                }
            }
        }

        public static void Generate(int nProc, string? filename = null, string? outdir = null)
        {
            if (nProc < 4)
            {
                throw new Exception("Cannot have less than 4 keys.");
            }

            if (nProc > 999)
            {
                throw new Exception("Cannot have more than 999 keys.");
            }

            string outd = Environment.CurrentDirectory;
            if (outdir != null)
            {
                Directory.CreateDirectory(outdir);
                outd = outdir;
            }

            var setupAddresses = new Dictionary<string, List<string>>
            {
                { "rmc", new() },
                { "fetch", new() },
                { "gossip", new() }
            };
            var addresses = new Dictionary<string, List<string>>
            {
                { "rmc", new() },
                { "mcast", new() },
                { "fetch", new() },
                { "gossip", new() }
            };

            if (filename is null)
            {
                for (int i = 0; i < nProc; i++)
                {
                    addresses["rmc"].Add($"127.0.0.1:{9000 + i}");
                    addresses["mcast"].Add($"127.0.0.1:{10000 + i}");
                    addresses["fetch"].Add($"127.0.0.1:{11000 + i}");
                    addresses["gossip"].Add($"127.0.0.1:{12000 + i}");
                    setupAddresses["rmc"].Add($"127.0.0.1:{13000 + i}");
                    setupAddresses["fetch"].Add($"127.0.0.1:{14000 + i}");
                    setupAddresses["gossip"].Add($"127.0.0.1:{15000 + i}");
                }
            }
            else
            {
                var f = File.ReadAllText(filename).Split("\n").Where(x => !string.IsNullOrEmpty(x)).ToArray();

                for (int pid = 0; pid < f.Length; pid++)
                {
                    var line = f[pid].Split('|');
                    if (line.Length < 2) throw new Exception("missing addresses");
                    var saddrs = line[0];
                    var addrs = line[1];
                    MakeAddressMap(saddrs, setupAddresses);
                    MakeAddressMap(addrs, addresses);
                }
            }

            var keys = Enumerable.Range(0, nProc).Select(_ => new MemberKeys(addresses)).ToArray();
            var committee = new Committee
            {
                SetupAddresses = setupAddresses,
                Addresses = addresses,
                PublicKeys = keys.Select(k => k.PublicKey).ToList(),
                RMCVerificationKeys = keys.Select(k => k.VerKey).ToList(),
                P2PPublicKeys = keys.Select(k => k.P2PPubKey).ToList()
            };

            for (int pid = 0; pid < nProc; pid++)
            {
                var member = new Member { Pid = (ushort)pid, P2PSecretKey = keys[pid].P2PSecKey, PrivateKey = keys[pid].PrivateKey, RMCSecretKey = keys[pid].SecKey };
                var fw = File.Create(Path.Combine(outd, $"{pid}.pk"));
                
                member.StoreMember(fw);

                fw.Flush();
                fw.Close();
            }

            var fc = File.Create(Path.Combine(outd, $"committee.ka"));

            committee.StoreCommittee(fc);

            fc.Flush();
            fc.Close();
        }
    }
}
