using Aurem.Model;
using Aurem.Serialize;
using AuremCore.Core.Extensions;
using AuremCore.Core;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aurem.Syncing.Internals.Packets;
using Aurem.Syncing.Internals.Packets.Bodies;
using Aurem.Syncing.Internals;
using System.Security.Cryptography;

namespace Aurem.Syncing
{
    public class FetchServer : IService
    {
        protected ushort Pid;
        protected IOrderer Orderer;
        protected Network Netserv;
        protected uint[] SyncIDs;
        protected CancellationTokenSource StopOut;
        protected Logger Log;
        protected long[] CurrentOut;
        protected Dictionary<ulong, bool> Orders;

        protected FetchServer(Config.Config conf, IOrderer orderer, Network netserv, Logger log)
        {
            Pid = conf.Pid;
            Orderer = orderer;
            Netserv = netserv;
            SyncIDs = new uint[conf.NProc];
            CurrentOut = new long[conf.NProc];
            StopOut = new CancellationTokenSource();
            Orders = new();
            Log = log;
        }

        public static (IService, Requests.Fetch) NewServer(Config.Config conf, IOrderer orderer, Network netserv, Logger log)
        {
            var s = new FetchServer(conf, orderer, netserv, log);
            return (s, s.Trigger);
        }

        public async Task<Exception?> Start()
        {
            await Netserv.Start();
            return null;
        }

        public async Task StopAsync()
        {
            StopOut.Cancel();
        }

        public async Task Trigger(ushort pid, ulong[] unitIDs)
        {
            if (Interlocked.Read(ref CurrentOut[pid]) > 10)
            {
                Log.Warn().Msg(Logging.Constants.RequestOverload);
                return;
            }

            var sid = Interlocked.Increment(ref SyncIDs[pid]);
            var p = new Packet(PacketID.FETCHREQUEST, new FetchRequestUnits(pid, sid, unitIDs));
            Orders.Add(((ulong)sid << 16) + pid, true);
            Netserv.Send(pid, p);
            Log.Info().Val(Logging.Constants.PID, pid).Val(Logging.Constants.OSID, sid).Msg(Logging.Constants.SyncStarted);
        }

        public async Task In(Packet p)
        {
            switch (p.Header.PacketID)
            {
                case (byte)PacketID.FETCHREQUEST:
                    {
                        var pb = (p.Body as FetchRequestUnits)!;
                        if (pb.Pid >= SyncIDs.Length)
                        {
                            Log.Warn().Val(Logging.Constants.PID, pb.Pid).Msg("Called by a stranger");

                            var log = Log.With().Val(Logging.Constants.PID, pb.Pid).Val(Logging.Constants.ISID, pb.Sid).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            IUnit[] units;
                            try
                            {
                                units = await Orderer.UnitsByID(pb.UnitIDs);
                            }
                            catch (Exception ex)
                            {
                                log.Error().Str("where", "Fetch.In.GetUnits").Msg(ex.Message);
                                return;
                            }

                            log.Debug().Val(Logging.Constants.Sent, units.Length).Msg(Logging.Constants.SendUnits);

                            var resp = new Packet(PacketID.FETCHRESPONSE, new FetchSendUnits(pb.Pid, pb.Sid, units));
                            Netserv.Send(pb.Pid, resp);

                            log.Info().Val(Logging.Constants.Sent, units.Length).Msg(Logging.Constants.SyncCompleted);
                        }
                    }
                    break;
                case (byte)PacketID.FETCHRESPONSE:
                    {
                        var pb = (p.Body as FetchSendUnits)!;

                        var log = Log.With().Val(Logging.Constants.PID, pb.Pid).Val(Logging.Constants.OSID, pb.Sid).Logger();

                        if (!Orders.ContainsKey(((ulong)pb.Sid << 16) + pb.Pid))
                        {
                            log.Warn().Msg("received unwanted fetch response");
                        }

                        var errs = await Orderer.AddPreunits(pb.Pid, pb.Units);
                        Logging.LoggingUtil.AddingErrors(errs, pb.Units.Length, log);

                        Orders.Remove(((ulong)pb.Sid << 16) + pb.Pid);

                        log.Info().Val(Logging.Constants.Recv, pb.Units.Length).Msg(Logging.Constants.SyncCompleted);
                        Interlocked.Decrement(ref CurrentOut[pb.Pid]);
                    }
                    break;
                default:
                    Log.Error().Msg("received wrong packet type");
                    break;
            }
        }
    }
}
