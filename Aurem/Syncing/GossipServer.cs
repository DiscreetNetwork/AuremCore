using Aurem.Logging;
using Aurem.Model;
using Aurem.Serialize;
using AuremCore.Core.Extensions;
using AuremCore.Core;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aurem.Syncing.Internals;
using Aurem.Syncing.Internals.Packets;
using Aurem.Random;
using Aurem.Syncing.Internals.Packets.Bodies;
using System.Security.Cryptography;

namespace Aurem.Syncing
{
    public class GossipServer : IService
    {
        protected ushort NProc;
        protected ushort Pid;
        protected IOrderer Orderer;
        protected Network Netserv;
        protected long Requests;
        protected uint[] SyncIDs;
        protected CancellationTokenSource StopOut;
        protected Logger Log;
        protected ConcurrentDictionary<ulong, Logger> SendState;
        protected ConcurrentDictionary<ulong, ReceiveStateValue> ReceiveState;

        protected class ReceiveStateValue
        {
            public Logger Log;
            public int Value;
        }

        protected GossipServer(Config.Config conf, IOrderer orderer, Network netserv, Logger log)
        {
            NProc = conf.NProc;
            Pid = conf.Pid;
            Orderer = orderer;
            Netserv = netserv;
            Requests = 0;
            SyncIDs = new uint[conf.NProc];
            StopOut = new CancellationTokenSource();
            Log = log;
            SendState = new();
            ReceiveState = new();

            Netserv.AddHandle(HandleGossip);
        }

        public static (IService, Requests.Gossip) NewServer(Config.Config conf, IOrderer orderer, Network netserv, Logger log)
        {
            var gossip = new GossipServer(conf, orderer, netserv, log);
            return (gossip, gossip.Request);
        }

        public async Task<Exception?> Start()
        {
            await Netserv.Start();
            return null;
        }

        public Task StopAsync()
        {
            StopOut.Cancel();
            // TODO: stop handling traffic
            return Task.CompletedTask;
        }

        public async Task Request(ushort pid)
        {
            if (Interlocked.Read(ref Requests) > NProc)
            {
                Log.Warn().Msg(Logging.Constants.RequestOverload);
                return;
            }

            var sid = SyncIDs[pid];
            SyncIDs[pid]++;

            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.OSID, sid).Logger();
            log.Info().Msg(Logging.Constants.SyncStarted);

            // handshake
            var dagInfo = await Orderer.GetInfo();
            log.Debug().Msg(Logging.Constants.SendInfo);
            var p = new Packet(PacketID.GOSSIPGREET, new GossipGreetPacket(Pid, sid, dagInfo));

            Netserv.Send(pid, p);
            SendState[GetStateKey(pid, sid)] = log;
            //await Console.Out.WriteLineAsync($"GOSSIP pid={Pid} remotePid={pid} sid={sid} added");
        }

        public static ulong GetStateKey(ushort pid, uint sid)
        {
            return ((ulong)sid << 16) + pid;
        }

        public async Task HandleGossip(Packet p)
        {
            //await Console.Out.WriteLineAsync($"GOSSIP GOSSIP GOSSIP: handling gossip type={(PacketID)p.Header.PacketID}");
            switch (p.Header.PacketID)
            {
                case (byte)PacketID.GOSSIPGREET:
                    {
                        var pb = (p.Body as GossipGreetPacket)!;
                        var log = Log.With().Val(Logging.Constants.PID, pb.Pid).Val(Logging.Constants.ISID, pb.Sid).Logger();
                        log.Info().Msg(Logging.Constants.SyncStarted);

                        var dagInfo = await Orderer.GetInfo();
                        var units = await Orderer.Delta(pb.DagInfo);
                        log.Debug().Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SendUnits);
                        var ps = new Packet(PacketID.GOSSIPINFO, new GossipInfoPacket(Pid, pb.Sid, dagInfo, units.ToArray()));

                        ReceiveState[GetStateKey(pb.Pid, pb.Sid)] = new ReceiveStateValue{ Log = log, Value = units.Count };
                        //await Console.Out.WriteLineAsync($"GOSSIP pid={Pid} remotePid={pb.Pid} sid={pb.Sid} added");
                        Netserv.Send(pb.Pid, ps);
                    }
                    break;
                case (byte)PacketID.GOSSIPINFO:
                    {
                        var pb = (p.Body as GossipInfoPacket)!;
                        var success = SendState.TryGetValue(GetStateKey(pb.Pid, pb.Sid), out var log);
                        if (!success)
                        {
                            Log.Warn().Msg("unknown gossip info packet received");
                            return;
                        }

                        var units = await Orderer.Delta(pb.DagInfo);
                        log.Debug().Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SendUnits);
                        var ps = new Packet(PacketID.GOSSIPUNITS, new GossipUnitsPacket(Pid, pb.Sid, units.ToArray()));

                        var errs = await Orderer.AddPreunits(pb.Pid, pb.Units);
                        LoggingUtil.AddingErrors(errs, pb.Units.Length, log);
                        log.Info().Val(Logging.Constants.Recv, pb.Units.Length).Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SyncCompleted);

                        Netserv.Send(pb.Pid, ps);
                        SendState.Remove(GetStateKey(pb.Pid, pb.Sid), out _);
                    }
                    break;
                case (byte)PacketID.GOSSIPUNITS:
                    {
                        var pb = (p.Body as GossipUnitsPacket)!;
                        var success = ReceiveState.TryGetValue(GetStateKey(pb.Pid, pb.Sid), out var logTup);
                        if (!success)
                        {
                            Log.Warn().Msg("unknown gossip units packet received");
                            return;
                        }

                        var log = logTup!.Log;
                        var len = logTup!.Value;

                        log.Debug().Msg(Logging.Constants.GetUnits);
                        var errs = await Orderer.AddPreunits(pb.Pid, pb.Units);
                        LoggingUtil.AddingErrors(errs, pb.Units.Length, log);
                        log.Info().Val(Logging.Constants.Recv, pb.Units.Length).Val(Logging.Constants.Sent, len).Msg(Logging.Constants.SyncCompleted);

                        Interlocked.Decrement(ref Requests);
                        ReceiveState.Remove(GetStateKey(pb.Pid, pb.Sid), out _);
                    }
                    break;
                default:
                    Log.Error().Msg("received wrong packet type");
                    return;
            }
        }
    }
}
