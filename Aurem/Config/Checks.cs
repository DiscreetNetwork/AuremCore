using Aurem.Dag.Checks;
using Aurem.Model;
using Aurem.Model.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Config
{
    /// <summary>
    /// Implements various checks for validity for the <see cref="Config"/> class.
    /// </summary>
    public static class Checks
    {
        /// <summary>
        /// The maximum allowed size of data in a unit, in bytes.
        /// </summary>
        public static readonly int MaxDataBytesPerUnit = 2_000_000;

        /// <summary>
        /// The maximum allowed size of random source data included in a unit, in bytes.
        /// </summary>
        public static readonly int MaxRandomSourceDataBytesPerUnit = 1_000_000;

        /// <summary>
        /// The maximum number of units in a chunk.
        /// </summary>
        public static readonly int MaxUnitsInChunk = 1_000_000;

        /// <summary>
        /// Default <see cref="UnitChecker"/> endpoints to use during setup phase.
        /// </summary>
        public static readonly UnitChecker[] SetupChecks = new UnitChecker[] { CheckUtil.BasicCorrectness, CheckUtil.ParentConsistency, CheckUtil.NoLevelSkipping, CheckUtil.NoForks };
        
        /// <summary>
        /// Default <see cref="UnitChecker"/> endpoints to use during consensus.
        /// </summary>
        public static readonly UnitChecker[] ConsensusChecks = new UnitChecker[] { CheckUtil.BasicCorrectness, CheckUtil.ParentConsistency, CheckUtil.NoSelfForkingEvidence, CheckUtil.ForkerMuting };

        private static void NoNulls<T>(IList<T> slice, ushort nProc, string keyType)
        {
            if (slice == null) throw new ConfigException(keyType + "is null");
            if (slice.Count != (int)nProc)
            {
                throw new ConfigException("wrong number of " + keyType);
            }

            for (int i = 0; i < nProc; i++)
            {
                if (slice[i] == null)
                {
                    throw new ConfigException(keyType + " contains a null value");
                }
            }
        }

        private static void CheckKeys(Config cnf)
        {
            if (cnf.NProc == 0)
            {
                throw new ConfigException("nProc set to 0 during keys check");
            }

            if (cnf.PrivateKey == null)
            {
                throw new ConfigException("Private key is missing");
            }

            NoNulls(cnf.PublicKeys, cnf.NProc, "Public keys");

            if (cnf.RMCPrivateKey == null)
            {
                throw new ConfigException("RMC private key is missing");
            }

            NoNulls(cnf.RMCPublicKeys, cnf.NProc, "RMC verification keys");

            if (cnf.P2PSecretKey == null)
            {
                throw new ConfigException("P2P private key is missing");
            }

            NoNulls(cnf.P2PPublicKeys, cnf.NProc, "P2P public keys");
        }

        private static void CheckSyncConf(Config cnf, bool setup)
        {
            if (cnf.Timeout == TimeSpan.Zero)
            {
                throw new ConfigException("timeout cannot be zero");
            }

            if (cnf.FetchInterval == TimeSpan.Zero)
            {
                throw new ConfigException("fetch interval cannot be zero");
            }

            if (cnf.GossipAbove == 0)
            {
                throw new ConfigException("GossipAbove cannot be zero");
            }

            var n = (int)cnf.NProc;
            var ok = (IList<string> s) =>
            {
                if (s == null && n == 0) return true;
                if (s == null) return false;
                return s.Count == n;
            };

            if (!ok(cnf.RMCAddresses)) throw new ConfigException("wrong number of RMC addresses");
            if (!ok(cnf.GossipAddresses)) throw new ConfigException("wrong number of gossip addresses");
            if (!ok(cnf.FetchAddresses)) throw new ConfigException("wrong number of fetch addresses");
            if (!setup) if (!ok(cnf.MCastAddresses)) throw new ConfigException("wrong number of mcast addresses");

            if (cnf.GossipWorkers == null || cnf.GossipWorkers.Length != 2) throw new ConfigException("Gossip workers is malformed");
            if (cnf.FetchWorkers == null || cnf.FetchWorkers.Length != 2) throw new ConfigException("Fetch workers is malformed");

            if (cnf.GossipWorkers[0] == 0) throw new ConfigException("nIn gossip workers cannot be zero");
            if (cnf.GossipWorkers[1] == 0) throw new ConfigException("nOut gossip workers cannot be zero");
            if (cnf.FetchWorkers[0] == 0) throw new ConfigException("nIn fetch workers cannot be zero");
            if (cnf.FetchWorkers[1] == 0) throw new ConfigException("nOut fetch workers cannot be zero");
        }

        private static string FuncName(object o) => o.GetType().Name;

        private static void CheckChecks(IList<UnitChecker> given, IList<UnitChecker> expected)
        {
            foreach (var sc in expected)
            {
                var notFound = true;
                var fn = FuncName(sc);
                foreach (var c in given)
                {
                    if (FuncName(c) == fn)
                    {
                        notFound = false;
                        break;
                    }
                }
                if (notFound)
                {
                    throw new ConfigException("missing check: " + FuncName(sc));
                }
            }
        }

        /// <summary>
        /// Checks basic validity of config for both setup and consensus.
        /// </summary>
        /// <param name="cnf"></param>
        /// <exception cref="ConfigException"></exception>
        private static void IsValid(Config cnf)
        {
            if (cnf.NProc < 4)
            {
                throw new ConfigException("nProc is " + cnf.NProc.ToString());
            }

            if (cnf.EpochLength < 1)
            {
                throw new ConfigException("EpochLength is " + cnf.EpochLength.ToString());
            }

            if (cnf.LogFile == "") throw new ConfigException("missing log filename");
            if (cnf.LogBuffer == 0) throw new ConfigException("Log buffer cannot be zero");

            CheckKeys(cnf);
        }

        /// <summary>
        /// Checks if a given config is valid for consensus.
        /// </summary>
        /// <param name="cnf"></param>
        public static void Valid(Config cnf)
        {
            IsValid(cnf);
            if (cnf.CRPFixedPrefix > cnf.NProc)
            {
                throw new ConfigException("CRPFixedPrefix cannot exceed NProc");
            }

            if (cnf.Checks.Count != ConsensusChecks.Length)
            {
                throw new ConfigException("wrong number of checks");
            }

            CheckChecks(cnf.Checks, ConsensusChecks);
            CheckSyncConf(cnf, false);
        }

        /// <summary>
        /// Checks if a given config is valid for setup.
        /// </summary>
        /// <param name="cnf"></param>
        public static void ValidSetup(Config cnf)
        {
            IsValid(cnf);
            if (cnf.CanSkipLevel)
            {
                throw new ConfigException("Cannot skip level in setup");
            }

            if (cnf.OrderStartLevel != 6)
            {
                throw new ConfigException("OrderStartLevel should be six; value was " + cnf.OrderStartLevel.ToString());
            }

            if (cnf.CRPFixedPrefix != 0)
            {
                throw new ConfigException("CRPFixedPrefix cannot be nonzero in setup");
            }

            if (cnf.Checks.Count != SetupChecks.Length)
            {
                throw new ConfigException("wrong number of checks");
            }

            CheckChecks(cnf.Checks, SetupChecks);
            CheckSyncConf(cnf, true);
        }
    }
}
