using Aurem.Model;
using AuremCore.Crypto.P2P;
using AuremCore.Crypto.Threshold;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Config
{
    /// <summary>
    /// Config represents a complete configuration needed for a process to start. 
    /// </summary>
    public class Config
    {
        public ushort Pid { get; set; }
        public ushort NProc { get; set; }

        // session
        public int Session { get; set; } = 0;
        public bool Sessioned { get; set; } = false;

        // epoch
        public int EpochLength { get; set; }
        public int NumberOfEpochs { get; set; }
        public int LastLevel { get; set; }  // = EpochLength + OrderStartLevel - 1
        public bool CanSkipLevel { get; set; }
        public List<UnitChecker> Checks { get; set; }

        // log
        public string LogFile { get; set; }
        public int LogLevel { get; set; }
        public bool LogHuman { get; set; }
        public int LogBuffer { get; set; }

        // keys
        public WeakThresholdKey WTKey { get; set; }
        public IPrivateKey PrivateKey { get; set; }
        public List<IPublicKey> PublicKeys { get; set; }
        public List<P2PPublicKey> P2PPublicKeys { get; set; }
        public P2PSecretKey P2PSecretKey { get; set; }
        public SecretKey RMCPrivateKey { get; set; }
        public List<VerificationKey> RMCPublicKeys { get; set; }

        // sync
        public int GossipAbove { get; set; }
        public TimeSpan FetchInterval { get; set; }
        public TimeSpan GossipInterval { get; set; }
        public TimeSpan Timeout { get; set; }
        public List<string> RMCAddresses { get; set; }
        public string RMCNetType { get; set; }
        public List<string> GossipAddresses { get; set; }
        public string GossipNetType { get; set; }   
        public List<string> FetchAddresses { get; set; }
        public string FetchNetType { get; set; }
        public List<string> MCastAddresses { get; set; }
        public string MCastNetType { get; set; }
        public int[] GossipWorkers { get; set; } // of size 2; nIn, nOut 
        public int[] FetchWorkers { get; set; } // same as above

        // linear
        public int OrderStartLevel { get; set; }
        public ushort CRPFixedPrefix { get; set; }
        public int ZeroVoteRoundForCommonVote { get; set; }
        public int FirstDecidingRound { get; set; }
        public int CommonVoteDeterministicPrefix { get; set; }

        // extra
        public bool IsLocal { get; set; } = false;

        public bool Setup { get; set; } = false;

        public void AddCheck(UnitChecker check)
        {
            if (Checks == null) Checks = new();
            Checks.Add(check);
        }

        private static Config RequiredByLinear()
        {
            return new Config { FirstDecidingRound = 3, CommonVoteDeterministicPrefix = 10, ZeroVoteRoundForCommonVote = 3 };
        }

        private static void AddLastLevel(Config cnf)
        {
            cnf.LastLevel = cnf.EpochLength + cnf.OrderStartLevel - 1;
        }

        private static void AddConsensusConf(Config cnf)
        {
            cnf.CanSkipLevel = true;
            cnf.OrderStartLevel = 0;
            cnf.CRPFixedPrefix = 4;
            cnf.EpochLength = 30;
            cnf.NumberOfEpochs = 3;
            cnf.Checks = Aurem.Config.Checks.ConsensusChecks.ToList();
            cnf.Setup = false;
        }

        private static void AddSetupConf(Config cnf)
        {
            cnf.CanSkipLevel = false;
            cnf.OrderStartLevel = 6;
            cnf.CRPFixedPrefix = 0;
            cnf.EpochLength = 1;
            cnf.NumberOfEpochs = 1;
            cnf.Checks = Aurem.Config.Checks.ConsensusChecks.ToList();
            cnf.Setup = true;
        }

        private static void AddLogConf(Config cnf, string logFile)
        {
            cnf.LogFile = logFile;
            cnf.LogBuffer = 100_000;
            cnf.LogHuman = false;
            cnf.LogLevel = -2;
        }

        private static void AddSyncConf(Config cnf, Dictionary<string, List<string>> addresses, bool setup)
        {
            cnf.Timeout = TimeSpan.FromSeconds(5);
            cnf.FetchInterval = TimeSpan.FromSeconds(1);
            cnf.GossipInterval = TimeSpan.FromMilliseconds(1000);
            cnf.GossipAbove = 50;

            cnf.RMCNetType = "tcp";
            cnf.RMCAddresses = addresses["rmc"];

            cnf.GossipNetType = "tcp";
            cnf.GossipAddresses = addresses["gossip"];

            cnf.FetchNetType = "tcp";
            cnf.FetchAddresses = addresses["fetch"];

            if (!setup)
            {
                cnf.MCastNetType = "tcp";
                cnf.MCastAddresses = addresses["mcast"];
            }

            var n = (int)cnf.NProc;
            cnf.GossipWorkers = new int[] { n / 20 + 1, n / 40 + 1 };
            cnf.FetchWorkers = new int[] { n / 2, n / 4 };
        }

        private static void AddKeys(Config cnf, Member m, Committee c)
        {
            cnf.Pid = m.Pid;
            cnf.NProc = (ushort)c.PublicKeys.Count;
            cnf.PrivateKey = m.PrivateKey;
            cnf.PublicKeys = c.PublicKeys;
            cnf.RMCPrivateKey = m.RMCSecretKey;
            cnf.RMCPublicKeys = c.RMCVerificationKeys;
            cnf.P2PSecretKey = m.P2PSecretKey;
            cnf.P2PPublicKeys = c.P2PPublicKeys;
        }

        public static Config Empty() => RequiredByLinear();

        public static Config NewSetup(Member m, Committee c)
        {
            var cnf = RequiredByLinear();

            AddKeys(cnf, m, c);
            AddSyncConf(cnf, c.SetupAddresses, true);
            AddLogConf(cnf, cnf.Pid.ToString() + ".setup");
            AddSetupConf(cnf);
            AddLastLevel(cnf);

            return cnf;
        }

        public static Config New(Member m, Committee c)
        {
            var cnf = RequiredByLinear();

            AddKeys(cnf, m, c);
            AddSyncConf(cnf, c.Addresses, false);
            AddLogConf(cnf, cnf.Pid.ToString());
            AddConsensusConf(cnf);
            AddLastLevel(cnf);

            return cnf;
        }
    }
}
